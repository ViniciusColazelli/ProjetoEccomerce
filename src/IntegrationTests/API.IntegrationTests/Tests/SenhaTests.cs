using API.IntegrationTests.Fixture;
using FluentAssertions;
using Infrastructure.Security.Criptografia;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.IntegrationTests.Tests
{
    // Serviço de senha: BCrypt para as senhas novas e o SHA-512 antigo ainda aceito na verificação
    public class SenhaTests : BaseTesteIntegracao
    {
        public SenhaTests(EcommerceFixture fixture) : base(fixture) { }

        private const int CUSTO = 4;
        private const string CHAVE = "chave-de-teste";
        private const string SENHA = "Senha@123";

        private static BCryptSenha CriarServico(int custo = CUSTO) => new(custo, new Sha512Encripter(CHAVE));

        // ════════════════════════════════════════════════════════════════
        // BCRYPT
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void Should_GerarHashBCryptComOCustoConfigurado()
        {
            // Act
            var hash = CriarServico().Gerar(SENHA);

            // Assert
            hash.Should().StartWith("$2").And.Contain($"${CUSTO:00}$").And.HaveLength(60);
            hash.Should().NotContain(SENHA);
        }

        // O sal é aleatório: a mesma senha gera hashes diferentes, e mesmo assim os dois verificam
        [Fact]
        public void Should_GerarHashDiferenteACadaVez_EVerificarAmbos()
        {
            // Arrange
            var servico = CriarServico();

            // Act
            var primeiro = servico.Gerar(SENHA);
            var segundo = servico.Gerar(SENHA);

            // Assert
            primeiro.Should().NotBe(segundo);
            servico.Verificar(SENHA, primeiro).Should().BeTrue();
            servico.Verificar(SENHA, segundo).Should().BeTrue();
        }

        [Theory]
        [InlineData("Senha@124")]
        [InlineData("senha@123")]
        [InlineData("")]
        public void Should_RecusarSenhaErrada_When_HashBCrypt(string senhaErrada)
        {
            // Arrange
            var servico = CriarServico();
            var hash = servico.Gerar(SENHA);

            // Act / Assert
            servico.Verificar(senhaErrada, hash).Should().BeFalse();
        }

        // ════════════════════════════════════════════════════════════════
        // FORMATO ANTIGO (SHA-512 + AdditionalKey)
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void Should_VerificarHashAntigo_When_ContaAindaNaoMigrada()
        {
            // Arrange
            var servico = CriarServico();
            var hashAntigo = new Sha512Encripter(CHAVE).Criptografia(SENHA);

            // Act / Assert
            servico.Verificar(SENHA, hashAntigo).Should().BeTrue();
            servico.Verificar("Senha@124", hashAntigo).Should().BeFalse();
        }

        // A AdditionalKey continua necessária para as contas antigas: com outra chave, a senha não confere
        [Fact]
        public void Should_RecusarHashAntigo_When_AdditionalKeyDiferente()
        {
            // Arrange
            var hashComOutraChave = new Sha512Encripter("outra-chave").Criptografia(SENHA);

            // Act / Assert
            CriarServico().Verificar(SENHA, hashComOutraChave).Should().BeFalse();
        }

        // ════════════════════════════════════════════════════════════════
        // QUANDO REGRAVAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void Should_PedirAtualizacao_When_HashAntigo()
        {
            // Arrange
            var hashAntigo = new Sha512Encripter(CHAVE).Criptografia(SENHA);

            // Act / Assert
            CriarServico().PrecisaAtualizar(hashAntigo).Should().BeTrue();
        }

        [Fact]
        public void Should_NaoPedirAtualizacao_When_BCryptComOMesmoCusto()
        {
            // Arrange
            var servico = CriarServico();

            // Act / Assert
            servico.PrecisaAtualizar(servico.Gerar(SENHA)).Should().BeFalse();
        }

        // Se um dia o custo de produção subir, os hashes com custo menor são regravados no login
        [Fact]
        public void Should_PedirAtualizacao_When_BCryptComCustoMenorQueOConfigurado()
        {
            // Arrange
            var hashCustoBaixo = CriarServico(custo: 4).Gerar(SENHA);

            // Act / Assert
            CriarServico(custo: 5).PrecisaAtualizar(hashCustoBaixo).Should().BeTrue();
        }

        // ════════════════════════════════════════════════════════════════
        // ENTRADAS INESPERADAS
        // ════════════════════════════════════════════════════════════════

        // E-mail sem conta: o login passa hash nulo, e a verificação recusa sem lançar erro
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Should_RecusarSemErro_When_SemHashSalvo(string? hashSalvo)
        {
            // Act / Assert
            CriarServico().Verificar(SENHA, hashSalvo).Should().BeFalse();
        }

        // Um hash corrompido no banco vira "senha errada", nunca erro 500
        [Theory]
        [InlineData("$2a$04$curto")]
        [InlineData("$2x$99$isso-nao-e-um-hash-bcrypt-valido-de-jeito-nenhum-aaaaaaaa")]
        public void Should_RecusarSemErro_When_HashBCryptCorrompido(string hashCorrompido)
        {
            // Arrange
            var servico = CriarServico();

            // Act / Assert
            servico.Verificar(SENHA, hashCorrompido).Should().BeFalse();
            servico.PrecisaAtualizar(hashCorrompido).Should().BeTrue();
        }

        // ════════════════════════════════════════════════════════════════
        // COMPATIBILIDADE COM O POSTGRES
        // ════════════════════════════════════════════════════════════════

        // Senha redefinida direto no Supabase (crypt + gen_salt('bf'), da extensão pgcrypto) precisa funcionar no login
        [Fact]
        public async Task Should_VerificarHashGeradoPeloPgcrypto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var scope = _fixture.CriarScope();

            var hashDoPostgres = await _fixture.GetDbContext(scope).Database
                .SqlQueryRaw<string>($"SELECT crypt('{SENHA}', gen_salt('bf', {CUSTO})) AS \"Value\"")
                .SingleAsync(ct);

            // Act / Assert
            hashDoPostgres.Should().StartWith("$2a$");
            CriarServico().Verificar(SENHA, hashDoPostgres).Should().BeTrue();
            CriarServico().Verificar("Senha@124", hashDoPostgres).Should().BeFalse();
        }

        // ════════════════════════════════════════════════════════════════
        // CONFIGURAÇÃO DA API
        // ════════════════════════════════════════════════════════════════

        // O serviço registrado na API gera BCrypt (com o custo 4 dos testes)
        [Fact]
        public void Should_RegistrarServicoBCryptNaApi()
        {
            // Arrange
            using var scope = _fixture.CriarScope();

            // Act
            var hash = _fixture.GetCriptografia(scope).Gerar(SENHA);

            // Assert
            hash.Should().StartWith("$2").And.Contain("$04$");
        }
    }
}
