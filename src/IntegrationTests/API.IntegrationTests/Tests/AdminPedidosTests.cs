using API.IntegrationTests.Fixture;
using Application.Response;
using Domain.Enums;
using Domain.Security.Perfis;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using static API.IntegrationTests.Fixture.CenarioPedidos;

namespace API.IntegrationTests.Tests
{
    public class AdminPedidosTests : BaseTesteIntegracao
    {
        public AdminPedidosTests(EcommerceFixture fixture) : base(fixture) { }

        private const string Endpoint = "/api/admin/pedidos";

        // ════════════════════════════════════════════════════════════════
        // AUTORIZAÇÃO
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(Endpoint)]
        [InlineData($"{Endpoint}/1")]
        public async Task Should_Return401_When_SemLogin(string url)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await client.GetAsync(url, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData(Endpoint)]
        [InlineData($"{Endpoint}/1")]
        public async Task Should_Return403_When_PerfilCliente(string url)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);

            // Act
            var response = await client.GetAsync(url, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════
        // LISTAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ListarPedidosDeTodosOsClientes_MaisRecentesPrimeiro()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var (maria, joao) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(_fixture, ct);

            var antigo = await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Pago, Dia(1), atualizadoEm: Dia(2));
            var recente = await PrepararPedido(_fixture, ct, catalogo, joao, StatusPedido.AguardandoPagamento, Dia(5), quantidade: 3);

            // Act
            var response = await client.GetAsync(Endpoint, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = (await response.Content.ReadFromJsonAsync<ResponsePedidosAdmin>(EcommerceFixture.JsonOptions, ct))!;
            body.Total.Should().Be(2);
            body.TamanhoPagina.Should().Be(20);
            body.Pedidos.Select(p => p.Id).Should().Equal(recente, antigo);

            var primeiro = body.Pedidos[0];
            primeiro.Status.Should().Be(StatusPedido.AguardandoPagamento);
            primeiro.QuantidadePecas.Should().Be(3);
            primeiro.Total.Should().Be(3 * PRECO_BASE);
            primeiro.CriadoEm.Should().Be(Dia(5));
            primeiro.AtualizadoEm.Should().BeNull();
            primeiro.Cliente.Id.Should().Be(joao);
            primeiro.Cliente.Nome.Should().Be("João Pereira");
            primeiro.Cliente.Email.Should().Be("joao@empresa.com");

            body.Pedidos[1].AtualizadoEm.Should().Be(Dia(2));
        }

        [Theory]
        [InlineData("status=Pago")]
        [InlineData("status=pago")]   // sem diferenciar maiúsculas, como nos outros filtros
        public async Task Should_FiltrarPorStatus(string query)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var (maria, _) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(_fixture, ct);

            var pago = await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Pago, Dia(1));
            await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.AguardandoPagamento, Dia(2));
            await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Cancelado, Dia(3));

            // Act
            var body = await Listar(client, query, ct);

            // Assert
            body.Total.Should().Be(1);
            body.Pedidos.Should().ContainSingle().Which.Id.Should().Be(pago);
        }

        [Theory]
        [InlineData("cliente=maria")]        // parte do nome, sem diferenciar maiúsculas
        [InlineData("cliente=SOUZA")]
        [InlineData("cliente=maria@email")]  // parte do e-mail
        public async Task Should_FiltrarPorCliente(string query)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var (maria, joao) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(_fixture, ct);

            var daMaria = await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Pago, Dia(1));
            await PrepararPedido(_fixture, ct, catalogo, joao, StatusPedido.Pago, Dia(2));

            // Act
            var body = await Listar(client, query, ct);

            // Assert
            body.Total.Should().Be(1);
            body.Pedidos.Should().ContainSingle().Which.Id.Should().Be(daMaria);
        }

        [Theory]
        [InlineData("cliente=joao%20pereira")]  // sem acento encontra "João Pereira" (o e-mail não tem o sobrenome)
        [InlineData("cliente=JO%C3%83O%20P")]   // "JOÃO P": com acento e maiúsculas também
        public async Task Should_FiltrarPorCliente_SemDiferenciarAcentos(string query)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var (maria, joao) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(_fixture, ct);

            await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Pago, Dia(1));
            var doJoao = await PrepararPedido(_fixture, ct, catalogo, joao, StatusPedido.Pago, Dia(2));

            // Act
            var body = await Listar(client, query, ct);

            // Assert
            body.Total.Should().Be(1);
            body.Pedidos.Should().ContainSingle().Which.Id.Should().Be(doJoao);
        }

        [Fact]
        public async Task Should_FiltrarPorPeriodo_IncluindoODiaFinalInteiro()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var (maria, _) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(_fixture, ct);

            await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Pago, Dia(1));
            var dia2 = await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Pago, Dia(2));
            var fimDoDia3 = await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Pago, new DateTime(2026, 10, 3, 23, 59, 0, DateTimeKind.Utc));
            await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Pago, Dia(4));

            // Act
            var body = await Listar(client, "de=2026-10-02&ate=2026-10-03", ct);

            // Assert
            body.Pedidos.Select(p => p.Id).Should().Equal(fimDoDia3, dia2);
        }

        [Fact]
        public async Task Should_CombinarFiltrosEPaginar()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var (maria, joao) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(_fixture, ct);

            var maisAntigo = await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Pago, Dia(1));
            await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Pago, Dia(2));
            await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Pago, Dia(3));
            await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.Cancelado, Dia(4));
            await PrepararPedido(_fixture, ct, catalogo, joao, StatusPedido.Pago, Dia(5));

            // Act
            var body = await Listar(client, "status=Pago&cliente=maria&pagina=2&tamanhoPagina=2", ct);

            // Assert
            body.Total.Should().Be(3);
            body.Pedidos.Should().ContainSingle().Which.Id.Should().Be(maisAntigo);
        }

        [Theory]
        [InlineData("status=Pendente", "Status inválido. Use AguardandoPagamento, Pago, EmProducao, ProntoParaRetirada, Entregue ou Cancelado.")]
        [InlineData("de=2026-10-10&ate=2026-10-01", "A data inicial deve ser menor ou igual à data final.")]
        [InlineData("pagina=0", "A página deve ser maior ou igual a 1.")]
        [InlineData("tamanhoPagina=51", "O tamanho da página deve estar entre 1 e 50.")]
        public async Task Should_Return400_When_FiltroInvalido(string query, string mensagemEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.GetAsync($"{Endpoint}?{query}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await MensagensDeErro(response, ct)).Should().Equal(mensagemEsperada);
        }

        [Fact]
        public async Task Should_Return400_When_BuscaPorClienteMuitoLonga()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.GetAsync($"{Endpoint}?cliente={new string('a', 201)}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await MensagensDeErro(response, ct)).Should().Equal("A busca por cliente deve ter no máximo 200 caracteres.");
        }

        // ════════════════════════════════════════════════════════════════
        // DETALHE
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ReturnDetalheComCliente_DeQualquerCliente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var (maria, _) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(_fixture, ct);
            var pedidoId = await PrepararPedido(_fixture, ct, catalogo, maria, StatusPedido.EmProducao, Dia(1), quantidade: 2, comBordado: true);

            // Act
            var response = await client.GetAsync($"{Endpoint}/{pedidoId}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = (await response.Content.ReadFromJsonAsync<ResponsePedidoAdmin>(EcommerceFixture.JsonOptions, ct))!;
            body.Id.Should().Be(pedidoId);
            body.Status.Should().Be(StatusPedido.EmProducao);
            body.Total.Should().Be(2 * (PRECO_BASE + PRECO_BORDADO));
            body.Cliente.Id.Should().Be(maria);
            body.Cliente.Nome.Should().Be("Maria Souza");
            body.Cliente.Email.Should().Be("maria@email.com");

            var item = body.Itens.Should().ContainSingle().Subject;
            item.Tamanho.Should().Be("M");
            item.Cor.Should().Be("Azul");
            item.Personalizacoes.Should().ContainSingle().Which.Valor.Should().Be("Bordado");
        }

        [Fact]
        public async Task Should_Return404_When_PedidoInexistente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.GetAsync($"{Endpoint}/999999", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await MensagensDeErro(response, ct)).Should().Equal("Pedido não encontrado.");
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        private async Task<(int Maria, int Joao)> PrepararClientes()
        {
            var maria = await _fixture.CadastrarCliente("maria@email.com", "Senha@123", nome: "Maria Souza");
            var joao = await _fixture.CadastrarCliente("joao@empresa.com", "Senha@123", nome: "João Pereira");

            return (maria.Id, joao.Id);
        }

        private static async Task<ResponsePedidosAdmin> Listar(HttpClient client, string query, CancellationToken ct)
        {
            var response = await client.GetAsync($"{Endpoint}?{query}", ct);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            return (await response.Content.ReadFromJsonAsync<ResponsePedidosAdmin>(EcommerceFixture.JsonOptions, ct))!;
        }

        private static async Task<List<string?>> MensagensDeErro(HttpResponseMessage response, CancellationToken ct)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            return body.GetProperty("errors").EnumerateArray().Select(erro => erro.GetString()).ToList();
        }
    }
}
