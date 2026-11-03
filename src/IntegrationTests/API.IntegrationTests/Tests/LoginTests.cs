using API.Extensions;
using API.IntegrationTests.Fixture;
using Application.Request;
using Application.Response;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    public class LoginTests : BaseTesteIntegracao
    {
        private const string ENDPOINT = "/api/login";
        private const string ENDPOINT_PERFIL = "/api/cliente";
        private const string COOKIE = "access_token";

        private const string EMAIL = "maria@email.com";
        private const string SENHA = "Senha@123";

        public LoginTests(EcommerceFixture fixture) : base(fixture) { }

        // ════════════════════════════════════════════════════════════════
        // LOGIN
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return200ComNomeESemTokenNoCorpo_When_CredenciaisValidas()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA, nome: "Maria Souza");
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await client.PostAsJsonAsync(ENDPOINT, new RequestLoginCliente { Email = EMAIL, Senha = SENHA }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseClienteLogado>(ct);
            body!.Nome.Should().Be("Maria Souza");
            body.Token.Should().BeEmpty();
        }

        [Fact]
        public async Task Should_GravarCookieHttpOnly_When_LoginValido()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await client.PostAsJsonAsync(ENDPOINT, new RequestLoginCliente { Email = EMAIL, Senha = SENHA }, ct);

            // Assert
            var cookie = ObterSetCookie(response);
            cookie.Should().StartWith($"{COOKIE}=");
            ValorDoCookie(cookie).Should().NotBeEmpty();
            cookie.Should().ContainEquivalentOf("httponly");
            cookie.Should().ContainEquivalentOf("samesite=lax");
            cookie.Should().ContainEquivalentOf("path=/");
            cookie.Should().ContainEquivalentOf("expires=");
        }

        [Theory]
        [InlineData(EMAIL, "SenhaErrada")]
        [InlineData("naoexiste@email.com", SENHA)]
        public async Task Should_Return401SemCookie_When_CredenciaisInvalidas(string email, string senha)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await client.PostAsJsonAsync(ENDPOINT, new RequestLoginCliente { Email = email, Senha = senha }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.Headers.Contains("Set-Cookie").Should().BeFalse();

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Email e/ou senha invalido");
        }

        [Fact]
        public async Task Should_Logar_When_EmailComOutraCapitalizacaoEEspacos()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await client.PostAsJsonAsync(ENDPOINT, new RequestLoginCliente { Email = "  Maria@EMAIL.com ", Senha = SENHA }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // O rate limiter fica desligado no ambiente Testing (todas as requisições vêm do mesmo IP),
        // então o teste confere que a política estrita está ligada nas rotas de autenticação
        [Theory]
        [InlineData("Login", "Login")]
        [InlineData("Cliente", "Registrar")]
        public void Should_AplicarLimiteDeAutenticacao(string controller, string action)
        {
            // Arrange
            using var scope = _fixture.CriarScope();
            var acoes = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items;

            // Act
            var acao = acoes.OfType<ControllerActionDescriptor>()
                .Single(a => a.ControllerName == controller && a.ActionName == action);

            // Assert
            acao.EndpointMetadata.OfType<EnableRateLimitingAttribute>()
                .Should().ContainSingle(atributo => atributo.PolicyName == RateLimitExtension.PoliticaAutenticacao);
        }

        // ════════════════════════════════════════════════════════════════
        // AUTENTICAÇÃO PELO COOKIE (o caminho que o front usa)
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_AutenticarPeloCookie_When_SemHeaderAuthorization()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA, nome: "Maria Souza");
            using var client = _fixture.CriarClientSemCookies();

            var login = await client.PostAsJsonAsync(ENDPOINT, new RequestLoginCliente { Email = EMAIL, Senha = SENHA }, ct);
            var token = ValorDoCookie(ObterSetCookie(login));

            // Act
            var response = await GetComCookie(client, ENDPOINT_PERFIL, token, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseClienteProfile>(ct);
            body!.Email.Should().Be(EMAIL);
            body.Nome.Should().Be("Maria Souza");
        }

        [Fact]
        public async Task Should_Return401_When_SemTokenNemCookie()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await client.GetAsync(ENDPOINT_PERFIL, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Cliente não autenticado ou sessão inválida, tente novamente.");
        }

        [Fact]
        public async Task Should_Return401_When_CookieComTokenInvalido()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await GetComCookie(client, ENDPOINT_PERFIL, "token-adulterado", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════
        // LOGOUT
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ExpirarCookie_When_Logout()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await client.PostAsync($"{ENDPOINT}/logout", null, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var cookie = ObterSetCookie(response);
            ValorDoCookie(cookie).Should().BeEmpty();
            cookie.Should().ContainEquivalentOf("expires=Thu, 01 Jan 1970");
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static string ObterSetCookie(HttpResponseMessage response) =>
            response.Headers.GetValues("Set-Cookie").Single(valor => valor.StartsWith($"{COOKIE}="));

        // "access_token=abc; expires=...; path=/" → "abc"
        private static string ValorDoCookie(string setCookie) =>
            setCookie.Split(';')[0][(COOKIE.Length + 1)..];

        private static Task<HttpResponseMessage> GetComCookie(HttpClient client, string url, string token, CancellationToken ct)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", $"{COOKIE}={token}");

            return client.SendAsync(request, ct);
        }
    }
}
