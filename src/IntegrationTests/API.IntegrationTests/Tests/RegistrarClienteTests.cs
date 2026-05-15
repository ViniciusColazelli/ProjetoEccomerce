using API.IntegrationTests.Fixture;
using Application.Request;
using Application.Response;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
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

            // Senha não foi salva em texto puro
            clienteSalvo!.Senha.Should().NotBe(senhaOriginal,
                because: "a senha deve ser criptografada antes de persistir");

            // Hash salvo é idêntico ao gerado pelo serviço real com a mesma AdditionalKey
            var criptografia = _fixture.GetCriptografia(scope);
            var hashEsperado = criptografia.Criptografia(senhaOriginal);

            clienteSalvo.Senha.Should().Be(hashEsperado,
                because: "o hash salvo deve ser idêntico ao gerado pelo serviço real");
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
