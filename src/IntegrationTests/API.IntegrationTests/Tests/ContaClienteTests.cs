using API.IntegrationTests.Fixture;
using Application.Request;
using Application.Response;
using Domain.Security.Perfis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    public class ContaClienteTests : BaseTesteIntegracao
    {
        private const string ENDPOINT = "/api/cliente";
        private const string ENDPOINT_SENHA = "/api/cliente/change-password";
        private const string ENDPOINT_LOGIN = "/api/login";

        private const string EMAIL = "maria@email.com";
        private const string SENHA = "Senha@123";

        public ContaClienteTests(EcommerceFixture fixture) : base(fixture) { }

        // ════════════════════════════════════════════════════════════════
        // PERFIL
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ReturnPerfilDoClienteLogado()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA, nome: "Maria Souza");
            using var client = await _fixture.CriarClientLogado(EMAIL, SENHA);

            // Act
            var response = await client.GetAsync(ENDPOINT, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseClienteProfile>(ct);
            body!.Nome.Should().Be("Maria Souza");
            body.Email.Should().Be(EMAIL);
            body.Role.Should().Be(Perfis.Cliente);
        }

        [Fact]
        public async Task Should_ReturnRoleAdmin_When_PerfilAdmin()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.GetAsync(ENDPOINT, ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseClienteProfile>(ct);
            body!.Role.Should().Be(Perfis.Admin);
        }

        // ════════════════════════════════════════════════════════════════
        // ATUALIZAR DADOS
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return204AndAtualizar_When_DadosValidos()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var cliente = await _fixture.CadastrarCliente(EMAIL, SENHA, nome: "Maria Souza");
            using var client = await _fixture.CriarClientLogado(EMAIL, SENHA);
            var request = new RequestUpdateCliente { Nome = "Maria S. Lima", Email = "maria.lima@email.com" };

            // Act
            var response = await client.PutAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = _fixture.CriarScope();
            var salvo = await _fixture.GetDbContext(scope).Clientes.SingleAsync(c => c.Id == cliente.Id, ct);
            salvo.Nome.Should().Be("Maria S. Lima");
            salvo.Email.Should().Be("maria.lima@email.com");
        }

        [Fact]
        public async Task Should_Return204_When_MantemOMesmoEmail()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA, nome: "Maria Souza");
            using var client = await _fixture.CriarClientLogado(EMAIL, SENHA);
            var request = new RequestUpdateCliente { Nome = "Maria S. Lima", Email = EMAIL };

            // Act
            var response = await client.PutAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Should_Return400ENaoAlterar_When_EmailDeOutroCliente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var cliente = await _fixture.CadastrarCliente(EMAIL, SENHA);
            await _fixture.CadastrarCliente("joao@email.com", SENHA);
            using var client = await _fixture.CriarClientLogado(EMAIL, SENHA);
            var request = new RequestUpdateCliente { Nome = "Maria", Email = "joao@email.com" };

            // Act
            var response = await client.PutAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Ajuste os dados enviados e tente novamente.");

            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).Clientes.SingleAsync(c => c.Id == cliente.Id, ct)).Email.Should().Be(EMAIL);
        }

        [Fact]
        public async Task Should_Return204ESalvarEmMinusculas_When_MudaSoACapitalizacaoDoEmail()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var cliente = await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = await _fixture.CriarClientLogado(EMAIL, SENHA);

            // Act
            var response = await client.PutAsJsonAsync(ENDPOINT, new RequestUpdateCliente { Nome = "Maria", Email = "Maria@Email.com" }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).Clientes.SingleAsync(c => c.Id == cliente.Id, ct)).Email.Should().Be(EMAIL);
        }

        [Fact]
        public async Task Should_Return400_When_EmailDeOutroClienteComOutraCapitalizacao()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            await _fixture.CadastrarCliente("joao@email.com", SENHA);
            using var client = await _fixture.CriarClientLogado(EMAIL, SENHA);

            // Act
            var response = await client.PutAsJsonAsync(ENDPOINT, new RequestUpdateCliente { Nome = "Maria", Email = "JOAO@email.com" }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData("", EMAIL, "Nome não pode ser vazio")]
        [InlineData("Maria", "", "E-mail não pode ser vazio")]
        [InlineData("Maria", "email-invalido", "O e-mail é invalido")]
        public async Task Should_Return400_When_AtualizacaoInvalida(string nome, string email, string mensagemEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = await _fixture.CriarClientLogado(EMAIL, SENHA);

            // Act
            var response = await client.PutAsJsonAsync(ENDPOINT, new RequestUpdateCliente { Nome = nome, Email = email }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors").EnumerateArray().Select(e => e.GetString()).Should().Contain(mensagemEsperada);
        }

        // ════════════════════════════════════════════════════════════════
        // TROCAR SENHA
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return204ELogarSoComNovaSenha_When_TrocaValida()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = await _fixture.CriarClientLogado(EMAIL, SENHA);
            var request = new RequestTrocarSenha { Senha = SENHA, NovaSenha = "NovaSenha@456" };

            // Act
            var response = await client.PutAsJsonAsync(ENDPOINT_SENHA, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var anonimo = _fixture.CriarClientSemCookies();
            var loginNova = await anonimo.PostAsJsonAsync(ENDPOINT_LOGIN, new RequestLoginCliente { Email = EMAIL, Senha = "NovaSenha@456" }, ct);
            var loginAntiga = await anonimo.PostAsJsonAsync(ENDPOINT_LOGIN, new RequestLoginCliente { Email = EMAIL, Senha = SENHA }, ct);

            loginNova.StatusCode.Should().Be(HttpStatusCode.OK);
            loginAntiga.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Conta ainda com senha antiga (SHA-512) troca a senha: a atual é aceita e a nova vai em BCrypt
        [Fact]
        public async Task Should_TrocarSenha_When_ContaComSenhaAntiga()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var cliente = await _fixture.CadastrarClienteComSenhaAntiga(EMAIL, SENHA);

            // autenticado pelo token (não pelo login), para a senha antiga chegar intacta na troca
            using var client = _fixture.CriarClientComToken(cliente);

            // Act
            var response = await client.PutAsJsonAsync(ENDPOINT_SENHA, new RequestTrocarSenha { Senha = SENHA, NovaSenha = "NovaSenha@456" }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var hash = await _fixture.SenhaGravada(cliente.Id);
            hash.Should().StartWith("$2");

            using var anonimo = _fixture.CriarClientSemCookies();
            var login = await anonimo.PostAsJsonAsync(ENDPOINT_LOGIN, new RequestLoginCliente { Email = EMAIL, Senha = "NovaSenha@456" }, ct);
            login.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Should_Return400EManterSenha_When_SenhaAtualErrada()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = await _fixture.CriarClientLogado(EMAIL, SENHA);
            var request = new RequestTrocarSenha { Senha = "SenhaErrada", NovaSenha = "NovaSenha@456" };

            // Act
            var response = await client.PutAsJsonAsync(ENDPOINT_SENHA, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Não foi possível alterar a senha. Verifique sua senha atual e tente novamente.");

            using var anonimo = _fixture.CriarClientSemCookies();
            var login = await anonimo.PostAsJsonAsync(ENDPOINT_LOGIN, new RequestLoginCliente { Email = EMAIL, Senha = SENHA }, ct);
            login.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData("", "Senha não pode ser vazia")]
        [InlineData("12345", "A senha deve possuir pelo menos 6 caracteres")]
        public async Task Should_Return400_When_NovaSenhaInvalida(string novaSenha, string mensagemEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = await _fixture.CriarClientLogado(EMAIL, SENHA);

            // Act
            var response = await client.PutAsJsonAsync(ENDPOINT_SENHA, new RequestTrocarSenha { Senha = SENHA, NovaSenha = novaSenha }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be(mensagemEsperada);
        }

        // ════════════════════════════════════════════════════════════════
        // SEM LOGIN
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("GET", ENDPOINT)]
        [InlineData("PUT", ENDPOINT)]
        [InlineData("PUT", ENDPOINT_SENHA)]
        public async Task Should_Return401_When_SemLogin(string metodo, string url)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = _fixture.CriarClientSemCookies();
            var request = new HttpRequestMessage(new HttpMethod(metodo), url)
            {
                Content = metodo == "PUT" ? JsonContent.Create(new { }) : null
            };

            // Act
            var response = await client.SendAsync(request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_Return401_When_ClienteDoTokenFoiApagado()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var cliente = await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = await _fixture.CriarClientLogado(EMAIL, SENHA);

            using (var scope = _fixture.CriarScope())
                await _fixture.GetDbContext(scope).Clientes.Where(c => c.Id == cliente.Id).ExecuteDeleteAsync(ct);

            // Act
            var response = await client.GetAsync(ENDPOINT, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Cliente não autenticado ou sessão inválida, tente novamente.");
        }
    }
}
