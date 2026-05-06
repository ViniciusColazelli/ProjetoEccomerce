using API.IntegrationTests.Factories;
using Application.Request;
using Application.Response;
using Domain.Security.Criptografia;
using FluentAssertions;
using Infrastructure.DataAcess;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    public class RegistrarClienteTests
    {
        private const string ENDPOINT = "/api/cliente";

        // ════════════════════════════════════════════════════════════════
        // FLUXO FELIZ
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return201_When_ClientIsCreatedSuccessfully()
        {
            // Arrange
            var factory = new CustomWebApplicationFactory(dbName: Guid.NewGuid().ToString());
            var client = factory.CreateClient();
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "João Silva",
                Email = "joao@email.com",
                Senha = "Senha@123"
            };

            // Act
            var response = await client.PostAsJsonAsync(ENDPOINT, request, ct);

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
            var factory = new CustomWebApplicationFactory(dbName: Guid.NewGuid().ToString());
            var client = factory.CreateClient();
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "Maria Souza",
                Email = "maria@email.com",
                Senha = "Senha@123"
            };

            // Act
            await client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert — acessa o banco InMemory diretamente para verificar persistência
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EccomerceDbContext>();
            var clienteSalvo = dbContext.Clientes.FirstOrDefault(c => c.Email == request.Email);

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
            var factory = new CustomWebApplicationFactory(dbName: Guid.NewGuid().ToString());
            var client = factory.CreateClient();
            var ct = TestContext.Current.CancellationToken;
            var senhaOriginal = "Senha@123";

            var request = new RequestRegistrarCliente
            {
                Nome = "Carlos Teste",
                Email = "carlos@email.com",
                Senha = senhaOriginal
            };

            // Act
            await client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert — senha salva deve ser diferente da original
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EccomerceDbContext>();
            var clienteSalvo = dbContext.Clientes.FirstOrDefault(c => c.Email == request.Email);

            clienteSalvo.Should().NotBeNull();
            clienteSalvo!.Senha.Should().NotBe(senhaOriginal,
                because: "a senha deve ser criptografada antes de persistir");

            // Verifica que o hash salvo é o correto usando o serviço real
            var criptografia = scope.ServiceProvider.GetRequiredService<ISenhaCriptografada>();
            var hashEsperado = criptografia.Criptografia(senhaOriginal);
            clienteSalvo.Senha.Should().Be(hashEsperado);
        }

        // ════════════════════════════════════════════════════════════════
        // VALIDAÇÃO: e-mail já cadastrado
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ReturnError_When_EmailAlreadyRegistered()
        {
            // Arrange — mesmo banco para os dois cadastros
            var factory = new CustomWebApplicationFactory(dbName: Guid.NewGuid().ToString());
            var client = factory.CreateClient();
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "Pedro Lima",
                Email = "pedro@email.com",
                Senha = "Senha@123"
            };

            // Primeiro cadastro — deve funcionar
            await client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Act — segundo cadastro com mesmo e-mail
            var response = await client.PostAsJsonAsync(ENDPOINT, request, ct);

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
            var factory = new CustomWebApplicationFactory(dbName: Guid.NewGuid().ToString());
            var client = factory.CreateClient();
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "",
                Email = "teste@email.com",
                Senha = "Senha@123"
            };

            // Act
            var response = await client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Should_ReturnError_When_EmailIsInvalid()
        {
            // Arrange
            var factory = new CustomWebApplicationFactory(dbName: Guid.NewGuid().ToString());
            var client = factory.CreateClient();
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "Ana Clara",
                Email = "email-invalido",
                Senha = "Senha@123"
            };

            // Act
            var response = await client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Should_ReturnError_When_SenhaIsEmpty()
        {
            // Arrange
            var factory = new CustomWebApplicationFactory(dbName: Guid.NewGuid().ToString());
            var client = factory.CreateClient();
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "Beatriz Rocha",
                Email = "beatriz@email.com",
                Senha = ""
            };

            // Act
            var response = await client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Should_ReturnError_When_AllFieldsAreEmpty()
        {
            // Arrange
            var factory = new CustomWebApplicationFactory(dbName: Guid.NewGuid().ToString());
            var client = factory.CreateClient();
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "",
                Email = "",
                Senha = ""
            };

            // Act
            var response = await client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

    }
}
