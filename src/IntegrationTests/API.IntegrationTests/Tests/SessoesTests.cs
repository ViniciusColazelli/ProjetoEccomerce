using API.IntegrationTests.Fixture;
using Domain.Entities;
using Domain.Repositories;
using Domain.Security.Tokens;
using FluentAssertions;
using Infrastructure.Security.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests.Tests
{
    // Base do refresh token: gerador, configuração e a tabela sessoes (migration 18)
    public class SessoesTests : BaseTesteIntegracao
    {
        public SessoesTests(EcommerceFixture fixture) : base(fixture) { }

        private static readonly GeradorRefreshToken Gerador = new(new SessaoConfig());

        // ════════════════════════════════════════════════════════════════
        // GERADOR DO REFRESH TOKEN
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void Should_GerarTokenAleatorio_EGuardarSoOHash()
        {
            // Act
            var primeiro = Gerador.Criar(clienteId: 1);
            var segundo = Gerador.Criar(clienteId: 1);

            // Assert
            primeiro.Token.Should().MatchRegex("^[A-Za-z0-9_-]{43}$");   // 32 bytes em base64url, sem "="
            primeiro.Token.Should().NotBe(segundo.Token);

            primeiro.Sessao.TokenHash.Should().MatchRegex("^[0-9a-f]{64}$");
            primeiro.Sessao.TokenHash.Should().Be(Gerador.CalcularHash(primeiro.Token));
            primeiro.Sessao.TokenHash.Should().NotContain(primeiro.Token);
            primeiro.Sessao.ClienteId.Should().Be(1);
        }

        [Fact]
        public void Should_CalcularHashSha256()
        {
            // vetor de teste oficial do SHA-256
            Gerador.CalcularHash("abc").Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        }

        [Fact]
        public void Should_ContarExpiracaoELimiteMaximo_When_Criar()
        {
            // Act
            var sessao = Gerador.Criar(clienteId: 1).Sessao;

            // Assert
            sessao.ExpiraEm.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
            sessao.ExpiraMaximoEm.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
            sessao.RevogadoEm.Should().BeNull();
            sessao.Ativa(DateTime.UtcNow).Should().BeTrue();
        }

        [Fact]
        public void Should_HerdarLimiteMaximo_When_Renovar()
        {
            // Arrange
            var anterior = new Sessoes { ClienteId = 3, ExpiraMaximoEm = DateTime.UtcNow.AddDays(20) };

            // Act
            var nova = Gerador.Renovar(anterior);

            // Assert
            nova.Sessao.ClienteId.Should().Be(3);
            nova.Sessao.ExpiraMaximoEm.Should().Be(anterior.ExpiraMaximoEm);
            nova.Sessao.ExpiraEm.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
            nova.Sessao.TokenHash.Should().Be(Gerador.CalcularHash(nova.Token));
        }

        // perto do limite máximo, a renovação não ganha os 7 dias inteiros
        [Fact]
        public void Should_PararNoLimiteMaximo_When_RenovarPertoDoFim()
        {
            // Arrange
            var anterior = new Sessoes { ClienteId = 3, ExpiraMaximoEm = DateTime.UtcNow.AddDays(2) };

            // Act
            var nova = Gerador.Renovar(anterior);

            // Assert
            nova.Sessao.ExpiraEm.Should().Be(anterior.ExpiraMaximoEm);
        }

        [Fact]
        public void Should_SessaoInativa_When_RevogadaOuVencida()
        {
            var agora = DateTime.UtcNow;

            new Sessoes { ExpiraEm = agora.AddDays(1), RevogadoEm = agora }.Ativa(agora).Should().BeFalse();
            new Sessoes { ExpiraEm = agora }.Ativa(agora).Should().BeFalse();
            new Sessoes { ExpiraEm = agora.AddSeconds(1) }.Ativa(agora).Should().BeTrue();
        }

        // ════════════════════════════════════════════════════════════════
        // CONFIGURAÇÃO
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void Should_UsarSeteETrintaDias_When_AppsettingsDaApi()
        {
            // Act
            using var scope = _fixture.CriarScope();
            var config = scope.ServiceProvider.GetRequiredService<SessaoConfig>();

            // Assert
            config.ExpiracaoEmDias.Should().Be(7);
            config.DuracaoMaximaEmDias.Should().Be(30);
        }

        [Fact]
        public void Should_UsarPadrao_When_SemSecaoSessao()
        {
            // Act
            var config = SessaoConfig.Ler(Configuracao());

            // Assert
            config.ExpiracaoEmDias.Should().Be(7);
            config.DuracaoMaximaEmDias.Should().Be(30);
        }

        [Fact]
        public void Should_AceitarExpiracaoIgualAoMaximo()
        {
            // Act
            var config = SessaoConfig.Ler(Configuracao(expiracao: "1", maximo: "1"));

            // Assert
            config.ExpiracaoEmDias.Should().Be(1);
            config.DuracaoMaximaEmDias.Should().Be(1);
        }

        // melhor a API não subir do que emitir sessões com validade errada
        [Theory]
        [InlineData("0", "30")]
        [InlineData("-1", "30")]
        [InlineData("10", "5")]
        public void Should_RecusarConfiguracaoInvalida(string expiracao, string maximo)
        {
            // Act
            var act = () => SessaoConfig.Ler(Configuracao(expiracao, maximo));

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*Sessao:ExpiracaoEmDias*");
        }

        // ════════════════════════════════════════════════════════════════
        // REPOSITÓRIO E TABELA
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_GravarELerSessaoPeloHash_ComOCliente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var cliente = await _fixture.CadastrarCliente("maria@email.com", "Senha@123", nome: "Maria Souza");

            string token;
            using (var scope = _fixture.CriarScope())
            {
                var gerado = scope.ServiceProvider.GetRequiredService<IGeradorRefreshToken>().Criar(cliente.Id);
                token = gerado.Token;

                await scope.ServiceProvider.GetRequiredService<ISessaoRepository>().Adicionar(gerado.Sessao);
                await scope.ServiceProvider.GetRequiredService<ISalvarDBRepository>().Salvar();
            }

            // Act
            using var leitura = _fixture.CriarScope();
            var repositorio = leitura.ServiceProvider.GetRequiredService<ISessaoRepository>();
            var hash = leitura.ServiceProvider.GetRequiredService<IGeradorRefreshToken>().CalcularHash(token);

            var sessao = await repositorio.GetByTokenHash(hash);
            var inexistente = await repositorio.GetByTokenHash(Gerador.CalcularHash("outro-token"));

            // Assert
            sessao.Should().NotBeNull();
            sessao!.ClienteId.Should().Be(cliente.Id);
            sessao.Cliente.Email.Should().Be("maria@email.com");
            sessao.Cliente.Nome.Should().Be("Maria Souza");
            sessao.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            sessao.CriadoEm.Kind.Should().Be(DateTimeKind.Utc);
            sessao.ExpiraEm.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
            sessao.ExpiraMaximoEm.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
            sessao.RevogadoEm.Should().BeNull();
            sessao.SubstituidaPorId.Should().BeNull();

            inexistente.Should().BeNull();

            // o token em si nunca vai para o banco
            var dbContext = _fixture.GetDbContext(leitura);
            (await dbContext.Sessoes.AnyAsync(s => s.TokenHash == token, ct)).Should().BeFalse();
        }

        // rotação: revogar a anterior e criar a nova no mesmo Salvar(), já ligadas
        [Fact]
        public async Task Should_LigarSessaoAnteriorANova_When_RenovadaNoMesmoSalvar()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var cliente = await _fixture.CadastrarCliente("maria@email.com", "Senha@123");
            var (tokenAnterior, idAnterior) = await GravarSessao(cliente.Id);

            int idNova;
            using (var scope = _fixture.CriarScope())
            {
                var repositorio = scope.ServiceProvider.GetRequiredService<ISessaoRepository>();
                var anterior = (await repositorio.GetByTokenHash(Gerador.CalcularHash(tokenAnterior)))!;

                // Act
                var nova = Gerador.Renovar(anterior);
                anterior.RevogadoEm = DateTime.UtcNow;
                anterior.SubstituidaPor = nova.Sessao;
                await repositorio.Adicionar(nova.Sessao);
                await scope.ServiceProvider.GetRequiredService<ISalvarDBRepository>().Salvar();

                idNova = nova.Sessao.Id;
            }

            // Assert
            using var leitura = _fixture.CriarScope();
            var sessoes = await _fixture.GetDbContext(leitura).Sessoes.AsNoTracking().ToDictionaryAsync(s => s.Id, ct);

            sessoes.Should().HaveCount(2);
            sessoes[idAnterior].SubstituidaPorId.Should().Be(idNova);
            sessoes[idAnterior].Ativa(DateTime.UtcNow).Should().BeFalse();
            sessoes[idNova].Ativa(DateTime.UtcNow).Should().BeTrue();
            sessoes[idNova].ExpiraMaximoEm.Should().Be(sessoes[idAnterior].ExpiraMaximoEm);
        }

        [Fact]
        public async Task Should_RecusarHashRepetido()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var cliente = await _fixture.CadastrarCliente("maria@email.com", "Senha@123");
            await GravarSessao(cliente.Id, s => s.TokenHash = new string('a', 64));

            // Act
            var act = () => GravarSessao(cliente.Id, s => s.TokenHash = new string('a', 64));

            // Assert
            (await act.Should().ThrowAsync<DbUpdateException>())
                .WithInnerException<Npgsql.PostgresException>()
                .Which.ConstraintName.Should().Be("ux_sessoes_token_hash");
        }

        [Fact]
        public async Task Should_RecusarExpiracaoDepoisDoLimiteMaximo()
        {
            // Arrange
            var cliente = await _fixture.CadastrarCliente("maria@email.com", "Senha@123");

            // Act
            var act = () => GravarSessao(cliente.Id, s => s.ExpiraEm = s.ExpiraMaximoEm.AddSeconds(1));

            // Assert
            (await act.Should().ThrowAsync<DbUpdateException>())
                .WithInnerException<Npgsql.PostgresException>()
                .Which.ConstraintName.Should().Be("ck_sessoes_expiracao");
        }

        [Fact]
        public async Task Should_ListarSoAsAtivasDoCliente_DaMaisAntigaParaAMaisNova()
        {
            // Arrange
            var maria = await _fixture.CadastrarCliente("maria@email.com", "Senha@123");
            var joao = await _fixture.CadastrarCliente("joao@empresa.com", "Senha@123");

            var (_, primeira) = await GravarSessao(maria.Id);
            await GravarSessao(maria.Id, s => s.RevogadoEm = DateTime.UtcNow);
            await GravarSessao(maria.Id, Vencida);
            var (_, segunda) = await GravarSessao(maria.Id);
            await GravarSessao(joao.Id);

            // Act
            using var scope = _fixture.CriarScope();
            var ativas = await scope.ServiceProvider.GetRequiredService<ISessaoRepository>().ListarAtivas(maria.Id, DateTime.UtcNow);

            // Assert
            ativas.Select(s => s.Id).Should().Equal(primeira, segunda);
        }

        // as revogadas ficam até vencer, para o reuso de um token já trocado ainda ser reconhecido
        [Fact]
        public async Task Should_RemoverSoAsVencidasDoCliente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var maria = await _fixture.CadastrarCliente("maria@email.com", "Senha@123");
            var joao = await _fixture.CadastrarCliente("joao@empresa.com", "Senha@123");

            var (_, ativa) = await GravarSessao(maria.Id);
            var (_, revogada) = await GravarSessao(maria.Id, s => s.RevogadoEm = DateTime.UtcNow);
            await GravarSessao(maria.Id, Vencida);
            var (_, vencidaDoJoao) = await GravarSessao(joao.Id, Vencida);

            // Act
            using (var scope = _fixture.CriarScope())
            {
                await scope.ServiceProvider.GetRequiredService<ISessaoRepository>().RemoverVencidas(maria.Id, DateTime.UtcNow);
                await scope.ServiceProvider.GetRequiredService<ISalvarDBRepository>().Salvar();
            }

            // Assert
            using var leitura = _fixture.CriarScope();
            var restantes = await _fixture.GetDbContext(leitura).Sessoes.Select(s => s.Id).ToListAsync(ct);

            restantes.Should().BeEquivalentTo([ativa, revogada, vencidaDoJoao]);
        }

        [Fact]
        public async Task Should_ApagarSessoes_When_ClienteApagado()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var maria = await _fixture.CadastrarCliente("maria@email.com", "Senha@123");
            var joao = await _fixture.CadastrarCliente("joao@empresa.com", "Senha@123");
            await GravarSessao(maria.Id);
            await GravarSessao(maria.Id);
            var (_, doJoao) = await GravarSessao(joao.Id);

            // Act: direto no banco, para valer o ON DELETE CASCADE da migration
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            await dbContext.Clientes.Where(c => c.Id == maria.Id).ExecuteDeleteAsync(ct);

            // Assert
            (await dbContext.Sessoes.Select(s => s.Id).ToListAsync(ct)).Should().Equal(doJoao);
        }

        [Fact]
        public async Task Should_DesfazerLigacao_When_SessaoSeguinteApagada()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var maria = await _fixture.CadastrarCliente("maria@email.com", "Senha@123");
            var (_, seguinte) = await GravarSessao(maria.Id);
            var (_, anterior) = await GravarSessao(maria.Id, s =>
            {
                s.RevogadoEm = DateTime.UtcNow;
                s.SubstituidaPorId = seguinte;
            });

            // Act: direto no banco, para valer o ON DELETE SET NULL da migration
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            await dbContext.Sessoes.Where(s => s.Id == seguinte).ExecuteDeleteAsync(ct);

            // Assert
            var restante = await dbContext.Sessoes.AsNoTracking().SingleAsync(ct);
            restante.Id.Should().Be(anterior);
            restante.SubstituidaPorId.Should().BeNull();
        }

        // ════════════════════════════════════════════════════════════════
        // AUXILIARES
        // ════════════════════════════════════════════════════════════════

        private static void Vencida(Sessoes sessao)
        {
            sessao.ExpiraMaximoEm = DateTime.UtcNow.AddDays(-1);
            sessao.ExpiraEm = sessao.ExpiraMaximoEm;
        }

        private async Task<(string Token, int Id)> GravarSessao(int clienteId, Action<Sessoes>? ajustar = null)
        {
            var gerado = Gerador.Criar(clienteId);
            ajustar?.Invoke(gerado.Sessao);

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            dbContext.Sessoes.Add(gerado.Sessao);
            await dbContext.SaveChangesAsync();

            return (gerado.Token, gerado.Sessao.Id);
        }

        private static IConfiguration Configuracao(string? expiracao = null, string? maximo = null)
        {
            var valores = new Dictionary<string, string?>();

            if (expiracao is not null) valores["Sessao:ExpiracaoEmDias"] = expiracao;
            if (maximo is not null) valores["Sessao:DuracaoMaximaEmDias"] = maximo;

            return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        }
    }
}
