using API.Extensions;
using API.IntegrationTests.Fixture;
using Application.Request;
using Application.UseCases.Login;
using Domain.Entities;
using Domain.Security.Perfis;
using Domain.Security.Tokens;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    // Login com sessão, POST /api/Login/refresh (rotação e reuso), logout que revoga a sessão,
    // troca de senha que derruba as outras e a limpeza/limite de sessões no login
    public class RefreshTokenTests : BaseTesteIntegracao
    {
        private const string ENDPOINT_LOGIN = "/api/login";
        private const string ENDPOINT_REFRESH = "/api/login/refresh";
        private const string ENDPOINT_LOGOUT = "/api/login/logout";
        private const string ENDPOINT_PERFIL = "/api/cliente";

        private const string COOKIE_ACCESS = "access_token";
        private const string COOKIE_REFRESH = "refresh_token";

        private const string EMAIL = "maria@email.com";
        private const string SENHA = "Senha@123";

        public RefreshTokenTests(EcommerceFixture fixture) : base(fixture) { }

        // ════════════════════════════════════════════════════════════════
        // LOGIN
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_GravarCookieRefreshStrictECriarSessao_When_Login()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var login = await Logar(client);

            // Assert
            var cookie = Cookie(login.Resposta, COOKIE_REFRESH);
            cookie.HttpOnly.Should().BeTrue();
            cookie.SameSite.Should().Be(SameSiteMode.Strict);
            cookie.Path.ToString().Should().Be("/");
            cookie.Expires!.Value.UtcDateTime.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
            login.Refresh.Should().MatchRegex("^[A-Za-z0-9_-]{43}$");

            // o access_token continua como antes
            Cookie(login.Resposta, COOKIE_ACCESS).SameSite.Should().Be(SameSiteMode.Lax);

            var sessao = (await Sessoes()).Should().ContainSingle().Subject;
            sessao.TokenHash.Should().Be(Hash(login.Refresh));
            sessao.Ativa(DateTime.UtcNow).Should().BeTrue();
        }

        // cada aparelho ou navegador tem a sua sessão
        [Fact]
        public async Task Should_CriarUmaSessaoPorLogin()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();

            // Act
            await Logar(client);
            await Logar(client);

            // Assert
            (await Sessoes()).Should().HaveCount(2).And.OnlyContain(s => s.RevogadoEm == null);
        }

        // ════════════════════════════════════════════════════════════════
        // REFRESH
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_TrocarOsDoisCookies_When_RefreshValido()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente(EMAIL, SENHA, nome: "Maria Souza");
            using var client = _fixture.CriarClientSemCookies();
            var login = await Logar(client);

            // Act
            var response = await Renovar(client, login.Refresh);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var novoAccess = Cookie(response, COOKIE_ACCESS).Value.ToString();
            var novoRefresh = Cookie(response, COOKIE_REFRESH).Value.ToString();
            novoRefresh.Should().NotBe(login.Refresh);
            Cookie(response, COOKIE_REFRESH).SameSite.Should().Be(SameSiteMode.Strict);

            var perfil = await ComCookie(client, HttpMethod.Get, ENDPOINT_PERFIL, $"{COOKIE_ACCESS}={novoAccess}");
            perfil.StatusCode.Should().Be(HttpStatusCode.OK);
            (await perfil.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("nome").GetString().Should().Be("Maria Souza");

            // a anterior foi revogada e aponta para a nova, que herda o limite máximo
            var sessoes = await Sessoes();
            var anterior = sessoes.Single(s => s.TokenHash == Hash(login.Refresh));
            var nova = sessoes.Single(s => s.TokenHash == Hash(novoRefresh));

            anterior.RevogadoEm.Should().NotBeNull();
            anterior.SubstituidaPorId.Should().Be(nova.Id);
            nova.Ativa(DateTime.UtcNow).Should().BeTrue();
            nova.ExpiraMaximoEm.Should().Be(anterior.ExpiraMaximoEm);
        }

        // o JWT novo sai com os dados do banco: quem virou Admin não precisa logar de novo
        [Fact]
        public async Task Should_EmitirJwtComDadosAtuais_When_Refresh()
        {
            // Arrange
            var cliente = await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();
            var login = await Logar(client);

            (await ComCookie(client, HttpMethod.Get, "/api/admin/pedidos", $"{COOKIE_ACCESS}={login.Access}"))
                .StatusCode.Should().Be(HttpStatusCode.Forbidden);

            using (var scope = _fixture.CriarScope())
            {
                await _fixture.GetDbContext(scope).Clientes.Where(c => c.Id == cliente.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Perfil, Perfis.Admin), TestContext.Current.CancellationToken);
            }

            // Act
            var response = await Renovar(client, login.Refresh);

            // Assert
            var novoAccess = Cookie(response, COOKIE_ACCESS).Value.ToString();
            (await ComCookie(client, HttpMethod.Get, "/api/admin/pedidos", $"{COOKIE_ACCESS}={novoAccess}"))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Falha não apaga cookies: numa corrida entre abas, a outra pode ter acabado de gravar os novos
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("token-inventado")]
        public async Task Should_Return401SemMexerNosCookies_When_SemCookieOuTokenDesconhecido(string? refresh)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await Renovar(client, refresh);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.Headers.Contains("Set-Cookie").Should().BeFalse();

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Cliente não autenticado ou sessão inválida, tente novamente.");
        }

        [Fact]
        public async Task Should_Return401_When_SessaoVencida()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();
            var login = await Logar(client);

            await AjustarSessoes(s => s
                .SetProperty(x => x.ExpiraEm, DateTime.UtcNow.AddMinutes(-1))
                .SetProperty(x => x.ExpiraMaximoEm, DateTime.UtcNow.AddMinutes(-1)));

            // Act
            var response = await Renovar(client, login.Refresh);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.Headers.Contains("Set-Cookie").Should().BeFalse();
            (await Sessoes()).Should().ContainSingle();   // nenhuma sessão nova
        }

        // perto do limite máximo (30 dias desde o login), a renovação não ganha os 7 dias inteiros
        [Fact]
        public async Task Should_PararNoLimiteMaximo_When_RefreshPertoDoFim()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();
            var login = await Logar(client);

            var limite = DateTime.UtcNow.AddDays(1);
            await AjustarSessoes(s => s
                .SetProperty(x => x.ExpiraEm, limite)
                .SetProperty(x => x.ExpiraMaximoEm, limite));

            // Act
            var response = await Renovar(client, login.Refresh);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Cookie(response, COOKIE_REFRESH).Expires!.Value.UtcDateTime.Should().BeCloseTo(limite, TimeSpan.FromSeconds(1));

            var nova = (await Sessoes()).Single(s => s.RevogadoEm == null);
            nova.ExpiraEm.Should().Be(nova.ExpiraMaximoEm);
        }

        // ════════════════════════════════════════════════════════════════
        // REUSO DO REFRESH TOKEN
        // ════════════════════════════════════════════════════════════════

        // duas abas renovando juntas: a segunda chega com o token que a primeira acabou de trocar
        [Fact]
        public async Task Should_Return401SemDerrubarASessao_When_ReusoDentroDaTolerancia()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();
            var login = await Logar(client);
            var primeira = await Renovar(client, login.Refresh);
            var novoRefresh = Cookie(primeira, COOKIE_REFRESH).Value.ToString();

            // Act
            var reuso = await Renovar(client, login.Refresh);

            // Assert
            reuso.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            reuso.Headers.Contains("Set-Cookie").Should().BeFalse();
            (await Renovar(client, novoRefresh)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // token já trocado aparecendo depois: alguém copiou o cookie. Todas as sessões do cliente caem
        [Fact]
        public async Task Should_RevogarTodasAsSessoesDoCliente_When_ReusoDepoisDaTolerancia()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            await _fixture.CadastrarCliente("joao@empresa.com", SENHA);
            using var client = _fixture.CriarClientSemCookies();

            var celular = await Logar(client);
            var computador = await Logar(client);
            var doJoao = await Logar(client, "joao@empresa.com");

            var renovada = await Renovar(client, celular.Refresh);
            var celularRenovado = Cookie(renovada, COOKIE_REFRESH).Value.ToString();

            // a troca foi há 1 minuto: passou da tolerância de 30 s
            await AjustarSessoes(s => s.SetProperty(x => x.RevogadoEm, DateTime.UtcNow.AddMinutes(-1)), Hash(celular.Refresh));

            // Act
            var reuso = await Renovar(client, celular.Refresh);

            // Assert
            reuso.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await Renovar(client, celularRenovado)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await Renovar(client, computador.Refresh)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // o outro cliente não é afetado
            (await Renovar(client, doJoao.Refresh)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // as duas requisições saem com o mesmo token: só uma pode ganhar a renovação
        [Fact]
        public async Task Should_RenovarUmaVezSo_When_DoisRefreshAoMesmoTempo()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();
            var login = await Logar(client);

            // Act
            var respostas = await Task.WhenAll(Renovar(client, login.Refresh), Renovar(client, login.Refresh));

            // Assert
            respostas.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.NoContent, HttpStatusCode.Unauthorized]);
            (await Sessoes()).Count(s => s.Ativa(DateTime.UtcNow)).Should().Be(1);
        }

        // ════════════════════════════════════════════════════════════════
        // LOGOUT
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_RevogarSoASessaoDoCookie_When_Logout()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();
            var celular = await Logar(client);
            var computador = await Logar(client);

            // Act
            var response = await ComCookie(client, HttpMethod.Post, ENDPOINT_LOGOUT, $"{COOKIE_REFRESH}={celular.Refresh}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var revogada = (await Sessoes()).Single(s => s.TokenHash == Hash(celular.Refresh));
            revogada.RevogadoEm.Should().NotBeNull();
            revogada.SubstituidaPorId.Should().BeNull();

            // uma cópia do cookie não serve mais; o outro aparelho continua logado
            (await Renovar(client, celular.Refresh)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await Renovar(client, computador.Refresh)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // sessão encerrada por logout não é reuso: recusar o token não derruba as outras
        [Fact]
        public async Task Should_NaoRevogarAsOutras_When_TokenDeLogoutReapresentado()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();
            var celular = await Logar(client);
            var computador = await Logar(client);
            await ComCookie(client, HttpMethod.Post, ENDPOINT_LOGOUT, $"{COOKIE_REFRESH}={celular.Refresh}");

            // bem depois da tolerância de reuso
            await AjustarSessoes(s => s.SetProperty(x => x.RevogadoEm, DateTime.UtcNow.AddHours(-1)), Hash(celular.Refresh));

            // Act
            var response = await Renovar(client, celular.Refresh);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await Renovar(client, computador.Refresh)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("token-inventado")]
        public async Task Should_ApagarOsDoisCookies_When_LogoutSemSessaoValida(string? refresh)
        {
            // Arrange
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await ComCookie(client, HttpMethod.Post, ENDPOINT_LOGOUT, refresh is null ? null : $"{COOKIE_REFRESH}={refresh}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            foreach (var nome in new[] { COOKIE_ACCESS, COOKIE_REFRESH })
            {
                var cookie = Cookie(response, nome);
                cookie.Value.ToString().Should().BeEmpty();
                cookie.Expires!.Value.Year.Should().Be(1970);
            }
        }

        // ════════════════════════════════════════════════════════════════
        // TROCA DE SENHA
        // ════════════════════════════════════════════════════════════════

        // quem tinha a senha antiga perde o acesso; quem trocou continua logado com uma sessão nova
        [Fact]
        public async Task Should_DerrubarTodasAsSessoesEAbrirUmaNova_When_TrocaDeSenha()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();
            var celular = await Logar(client);
            var computador = await Logar(client);

            // Act
            var response = await TrocarSenha(client, computador.Access, SENHA, "NovaSenha@456");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var novoAccess = Cookie(response, COOKIE_ACCESS).Value.ToString();
            var novoRefresh = Cookie(response, COOKIE_REFRESH).Value.ToString();
            Cookie(response, COOKIE_REFRESH).SameSite.Should().Be(SameSiteMode.Strict);

            (await Renovar(client, celular.Refresh)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await Renovar(client, computador.Refresh)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            (await ComCookie(client, HttpMethod.Get, ENDPOINT_PERFIL, $"{COOKIE_ACCESS}={novoAccess}"))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await Renovar(client, novoRefresh)).StatusCode.Should().Be(HttpStatusCode.NoContent);

            // as antigas foram encerradas, não trocadas: não contam como reuso
            (await Sessoes()).Where(s => s.TokenHash == Hash(celular.Refresh) || s.TokenHash == Hash(computador.Refresh))
                .Should().HaveCount(2).And.OnlyContain(s => s.RevogadoEm != null && s.SubstituidaPorId == null);
        }

        [Fact]
        public async Task Should_ManterAsSessoes_When_SenhaAtualErrada()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();
            var celular = await Logar(client);
            var computador = await Logar(client);

            // Act
            var response = await TrocarSenha(client, computador.Access, "SenhaErrada@1", "NovaSenha@456");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Headers.Contains("Set-Cookie").Should().BeFalse();
            (await Sessoes()).Should().HaveCount(2).And.OnlyContain(s => s.RevogadoEm == null);
            (await Renovar(client, celular.Refresh)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // ════════════════════════════════════════════════════════════════
        // LIMPEZA E LIMITE NO LOGIN
        // ════════════════════════════════════════════════════════════════

        // as vencidas saem; as revogadas que ainda não venceram ficam (reuso continua reconhecível)
        [Fact]
        public async Task Should_ApagarSoAsVencidasDoCliente_When_Login()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            await _fixture.CadastrarCliente("joao@empresa.com", SENHA);
            using var client = _fixture.CriarClientSemCookies();

            var vencida = await Logar(client);
            var revogada = await Logar(client);
            var doJoao = await Logar(client, "joao@empresa.com");

            var ontem = DateTime.UtcNow.AddDays(-1);
            foreach (var hash in new[] { Hash(vencida.Refresh), Hash(doJoao.Refresh) })
                await AjustarSessoes(s => s.SetProperty(x => x.ExpiraEm, ontem).SetProperty(x => x.ExpiraMaximoEm, ontem), hash);
            await ComCookie(client, HttpMethod.Post, ENDPOINT_LOGOUT, $"{COOKIE_REFRESH}={revogada.Refresh}");

            // Act
            var nova = await Logar(client);

            // Assert
            (await Sessoes()).Select(s => s.TokenHash).Should()
                .BeEquivalentTo([Hash(revogada.Refresh), Hash(doJoao.Refresh), Hash(nova.Refresh)]);
        }

        [Fact]
        public async Task Should_RevogarAMaisAntiga_When_LoginPassaDoLimiteDeSessoes()
        {
            // Arrange
            await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();

            var logins = new List<Login>();
            for (var i = 0; i < LoginClienteUseCase.MAXIMO_SESSOES; i++)
                logins.Add(await Logar(client));

            (await Sessoes()).Count(s => s.RevogadoEm == null).Should().Be(LoginClienteUseCase.MAXIMO_SESSOES);

            // Act
            var excedente = await Logar(client);

            // Assert
            var sessoes = await Sessoes();
            sessoes.Count(s => s.RevogadoEm == null).Should().Be(LoginClienteUseCase.MAXIMO_SESSOES);
            sessoes.Single(s => s.RevogadoEm != null).TokenHash.Should().Be(Hash(logins[0].Refresh));

            // a revogada pelo limite não conta como reuso: as outras continuam valendo
            (await Renovar(client, logins[0].Refresh)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await Renovar(client, logins[1].Refresh)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await Renovar(client, excedente.Refresh)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // ════════════════════════════════════════════════════════════════
        // CONFIGURAÇÃO
        // ════════════════════════════════════════════════════════════════

        // O rate limiter fica desligado no ambiente Testing: o teste confere a política na rota
        [Fact]
        public void Should_AplicarLimiteDeRenovacao_NoRefresh()
        {
            // Arrange
            using var scope = _fixture.CriarScope();
            var acoes = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items;

            // Act
            var acao = acoes.OfType<ControllerActionDescriptor>()
                .Single(a => a.ControllerName == "Login" && a.ActionName == "Renovar");

            // Assert
            acao.EndpointMetadata.OfType<EnableRateLimitingAttribute>()
                .Should().ContainSingle(atributo => atributo.PolicyName == RateLimitExtension.PoliticaRenovacao);
        }

        // Com o padrão do .NET (5 minutos de tolerância) este token ainda passaria
        [Fact]
        public async Task Should_Recusar_When_JwtVencidoHaUmMinuto()
        {
            // Arrange
            var cliente = await _fixture.CadastrarCliente(EMAIL, SENHA);
            using var client = _fixture.CriarClientSemCookies();
            var token = JwtVencido(cliente, vencidoHa: TimeSpan.FromMinutes(1));

            // Act
            var response = await ComCookie(client, HttpMethod.Get, ENDPOINT_PERFIL, $"{COOKIE_ACCESS}={token}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ── Helpers ───────────────────────────────────────────────────

        private sealed record Login(HttpResponseMessage Resposta, string Access, string Refresh);

        private static async Task<Login> Logar(HttpClient client, string email = EMAIL)
        {
            var response = await client.PostAsJsonAsync(ENDPOINT_LOGIN, new RequestLoginCliente { Email = email, Senha = SENHA });
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            return new Login(response, Cookie(response, COOKIE_ACCESS).Value.ToString(), Cookie(response, COOKIE_REFRESH).Value.ToString());
        }

        private static Task<HttpResponseMessage> Renovar(HttpClient client, string? refresh) =>
            ComCookie(client, HttpMethod.Post, ENDPOINT_REFRESH, refresh is null ? null : $"{COOKIE_REFRESH}={refresh}");

        private static Task<HttpResponseMessage> ComCookie(HttpClient client, HttpMethod metodo, string url, string? cookie)
        {
            var request = new HttpRequestMessage(metodo, url);
            if (cookie is not null)
                request.Headers.Add("Cookie", cookie);

            return client.SendAsync(request);
        }

        private static Task<HttpResponseMessage> TrocarSenha(HttpClient client, string access, string senhaAtual, string novaSenha)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, "/api/cliente/change-password")
            {
                Content = JsonContent.Create(new RequestTrocarSenha { Senha = senhaAtual, NovaSenha = novaSenha })
            };
            request.Headers.Add("Cookie", $"{COOKIE_ACCESS}={access}");

            return client.SendAsync(request);
        }

        private static SetCookieHeaderValue Cookie(HttpResponseMessage response, string nome) =>
            SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList())
                .Single(cookie => cookie.Name == nome);

        private string Hash(string refresh)
        {
            using var scope = _fixture.CriarScope();
            return scope.ServiceProvider.GetRequiredService<IGeradorRefreshToken>().CalcularHash(refresh);
        }

        private async Task<List<Sessoes>> Sessoes()
        {
            using var scope = _fixture.CriarScope();
            return await _fixture.GetDbContext(scope).Sessoes.AsNoTracking().OrderBy(s => s.Id).ToListAsync();
        }

        // direto no banco: todas as sessões, ou só a do hash informado
        private async Task AjustarSessoes(Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<Sessoes>> ajustar, string? hash = null)
        {
            using var scope = _fixture.CriarScope();
            var sessoes = _fixture.GetDbContext(scope).Sessoes.AsQueryable();

            if (hash is not null)
                sessoes = sessoes.Where(s => s.TokenHash == hash);

            await sessoes.ExecuteUpdateAsync(ajustar);
        }

        // mesmo emissor, público e chave da API, mas vencido
        private string JwtVencido(Clientes cliente, TimeSpan vencidoHa)
        {
            using var scope = _fixture.CriarScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:ChaveSecreta"]!));
            var agora = DateTime.UtcNow;

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: [new Claim(ClaimTypes.NameIdentifier, cliente.Id.ToString()), new Claim(ClaimTypes.Role, cliente.Perfil)],
                notBefore: agora.AddMinutes(-30),
                expires: agora - vencidoHa,
                signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
