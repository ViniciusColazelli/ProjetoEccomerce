using API.IntegrationTests.Fixture;
using Application.Request;
using Application.Response;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    public class RegistrarClienteTests : BaseTesteIntegracao
    {
        private const string ENDPOINT = "/api/cliente";

        public RegistrarClienteTests(EcommerceFixture fixture) : base(fixture) { }

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

            // Hash BCrypt (com sal aleatório): não dá para comparar com outro hash, só verificar a senha contra ele
            clienteSalvo.Senha.Should().StartWith("$2",
                because: "senhas novas são gravadas em BCrypt");

            var criptografia = _fixture.GetCriptografia(scope);
            criptografia.Verificar(senhaOriginal, clienteSalvo.Senha).Should().BeTrue();
            criptografia.Verificar("Senha@124", clienteSalvo.Senha).Should().BeFalse();
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
            (await MensagensDeErro(response, ct)).Should().ContainSingle().Which.Should().Be("Ajuste os dados enviados e tente novamente.");
        }

        [Fact]
        public async Task Should_ReturnError_When_EmailJaRegistradoComOutraCapitalizacao()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente("pedro@email.com", "Senha@123");

            var request = new RequestRegistrarCliente
            {
                Nome = "Pedro Lima",
                Email = "Pedro@EMAIL.com",
                Senha = "Senha@123"
            };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Should_SalvarEmailEmMinusculasESemEspacos()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var request = new RequestRegistrarCliente
            {
                Nome = "Pedro Lima",
                Email = " Pedro@EMAIL.com ",
                Senha = "Senha@123"
            };

            // Act
            await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).Clientes.SingleAsync(ct)).Email.Should().Be("pedro@email.com");
        }

        // ════════════════════════════════════════════════════════════════
        // VALIDAÇÃO: dados inválidos
        // ════════════════════════════════════════════════════════════════

        // Cada caso confere a mensagem exata, e que nada foi gravado
        [Theory]
        [InlineData("", "teste@email.com", "Senha@123", "Nome não pode ser vazio")]
        [InlineData("Ana Clara", "email-invalido", "Senha@123", "O e-mail é invalido")]
        [InlineData("Beatriz Rocha", "beatriz@email.com", "", "Senha não pode ser vazia")]
        [InlineData("Beatriz Rocha", "beatriz@email.com", "12345", "A senha deve possuir pelo menos 6 caracteres")]
        public async Task Should_ReturnError_When_CampoInvalido(string nome, string email, string senha, string mensagemEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var request = new RequestRegistrarCliente { Nome = nome, Email = email, Senha = senha };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await MensagensDeErro(response, ct)).Should().ContainSingle().Which.Should().Be(mensagemEsperada);

            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).Clientes.CountAsync(ct)).Should().Be(0);
        }

        // O BCrypt só considera 72 bytes: acima disso o cadastro recusa, em vez de ignorar o final da senha
        [Theory]
        [InlineData(72, "a", HttpStatusCode.Created)]
        [InlineData(73, "a", HttpStatusCode.BadRequest)]
        [InlineData(37, "ç", HttpStatusCode.BadRequest)]   // 37 caracteres, mas 74 bytes ("ç" ocupa 2)
        public async Task Should_LimitarSenhaA72Bytes(int repeticoes, string caractere, HttpStatusCode statusEsperado)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var senha = string.Concat(Enumerable.Repeat(caractere, repeticoes));
            var request = new RequestRegistrarCliente { Nome = "Beatriz Rocha", Email = "beatriz@email.com", Senha = senha };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(statusEsperado);

            if (statusEsperado == HttpStatusCode.BadRequest)
                (await MensagensDeErro(response, ct)).Should().ContainSingle().Which.Should()
                    .Be("A senha é longa demais. Use no máximo 72 caracteres (letras acentuadas e emojis contam como mais de um).");
        }

        [Fact]
        public async Task Should_Return201_When_SenhaComExatamente6Caracteres()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var request = new RequestRegistrarCliente { Nome = "Beatriz Rocha", Email = "beatriz@email.com", Senha = "123456" };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task Should_ReturnTodosOsErros_When_AllFieldsAreEmpty()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var request = new RequestRegistrarCliente { Nome = "", Email = "", Senha = "" };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await MensagensDeErro(response, ct)).Should().Contain(new[]
            {
                "Nome não pode ser vazio",
                "E-mail não pode ser vazio",
                "Senha não pode ser vazia"
            });
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static async Task<List<string?>> MensagensDeErro(HttpResponseMessage response, CancellationToken ct)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            return body.GetProperty("errors").EnumerateArray().Select(erro => erro.GetString()).ToList();
        }
    }
}
