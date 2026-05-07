using API.IntegrationTests.Fixture;
using Application.Request;
using Application.Response;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    [Collection("SequentialTests")]
    public class RegistrarClienteTests : IClassFixture<EcommerceFixture>
    {
        private readonly EcommerceFixture _fixture;
        private const string ENDPOINT = "/api/cliente";

        public RegistrarClienteTests(EcommerceFixture fixture)
        {
            _fixture = fixture;
        }

        // ════════════════════════════════════════════════════════════════
        // FLUXO FELIZ
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return201_When_ClientIsCreatedSuccessfully()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "João Silva",
                Email = "joao@email.com",
                Senha = "Senha@123"
            };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseClienteRegistrado>(ct);
            body.Should().NotBeNull();
            body!.Nome.Should().Be(request.Nome);
        }

        // ════════════════════════════════════════════════════════════════
        // PERSISTÊNCIA
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_PersistCliente_When_RequestIsValid()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "Maria Souza",
                Email = "maria@email.com",
                Senha = "Senha@123"
            };

            // Act
            await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            var clienteSalvo = dbContext.Clientes
                .FirstOrDefault(c => c.Email == request.Email);

            clienteSalvo.Should().NotBeNull();
            clienteSalvo!.Nome.Should().Be(request.Nome);
            clienteSalvo.Email.Should().Be(request.Email);
        }

        // ════════════════════════════════════════════════════════════════
        // REGRA DE NEGÓCIO: senha criptografada
        // Valida que:
        // 1. A senha não foi salva em texto puro
        // 2. A senha salva tem o tamanho esperado de um SHA-512 (128 chars)
        // 3. A senha salva contém apenas caracteres hexadecimais
        // Dessa forma garantimos que a criptografia foi aplicada sem
        // depender de chaves externas para comparar o hash.
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_SaveEncryptedPassword_When_ClientIsCreated()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var senhaOriginal = "Senha@123";

            var request = new RequestRegistrarCliente
            {
                Nome = "Carlos Teste",
                Email = "carlos@email.com",
                Senha = senhaOriginal
            };

            // Act
            await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            var clienteSalvo = dbContext.Clientes
                .FirstOrDefault(c => c.Email == request.Email);

            clienteSalvo.Should().NotBeNull();

            // 1. Senha não foi salva em texto puro
            clienteSalvo!.Senha.Should().NotBe(senhaOriginal,
                because: "a senha deve ser criptografada antes de persistir");

            // 2. Tamanho esperado de um hash SHA-512 em hexadecimal = 128 caracteres
            clienteSalvo.Senha.Should().HaveLength(128,
                because: "um hash SHA-512 em hexadecimal deve ter exatamente 128 caracteres");

            // 3. Contém apenas caracteres hexadecimais (0-9, a-f)
            clienteSalvo.Senha.Should().MatchRegex("^[0-9a-f]{128}$",
                because: "o hash SHA-512 deve conter apenas caracteres hexadecimais minúsculos");
        }

        // ════════════════════════════════════════════════════════════════
        // VALIDAÇÃO: e-mail já cadastrado
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ReturnError_When_EmailAlreadyRegistered()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "Pedro Lima",
                Email = "pedro@email.com",
                Senha = "Senha@123"
            };

            // Primeiro cadastro — deve funcionar
            await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Act — segundo cadastro com mesmo e-mail
            var response = await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ════════════════════════════════════════════════════════════════
        // VALIDAÇÃO: dados inválidos
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ReturnError_When_NomeIsEmpty()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "",
                Email = "teste@email.com",
                Senha = "Senha@123"
            };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Should_ReturnError_When_EmailIsInvalid()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "Ana Clara",
                Email = "email-invalido",
                Senha = "Senha@123"
            };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Should_ReturnError_When_SenhaIsEmpty()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "Beatriz Rocha",
                Email = "beatriz@email.com",
                Senha = ""
            };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Should_ReturnError_When_AllFieldsAreEmpty()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "",
                Email = "",
                Senha = ""
            };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
