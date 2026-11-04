using API.IntegrationTests.Fixture;
using Application.Response;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using static API.IntegrationTests.Fixture.CenarioPedidos;

namespace API.IntegrationTests.Tests
{
    public class MeusPedidosTests : BaseTesteIntegracao
    {
        public MeusPedidosTests(EcommerceFixture fixture) : base(fixture) { }

        private const string Endpoint = "/api/pedidos";
        private const string SENHA = "Senha@123";

        // ════════════════════════════════════════════════════════════════
        // LISTAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ListarSoOsMeusPedidos_MaisRecentesPrimeiro()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var (eu, outro, client) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(ct);

            var primeiro = await PrepararPedido(ct, catalogo, eu, StatusPedido.Entregue, Dia(1), quantidade: 1);
            var terceiro = await PrepararPedido(ct, catalogo, eu, StatusPedido.AguardandoPagamento, Dia(3), quantidade: 4);
            var segundo = await PrepararPedido(ct, catalogo, eu, StatusPedido.Pago, Dia(2), quantidade: 2);
            await PrepararPedido(ct, catalogo, outro, StatusPedido.AguardandoPagamento, Dia(4), quantidade: 1);

            // Act
            var response = await client.GetAsync(Endpoint, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = (await response.Content.ReadFromJsonAsync<ResponsePedidos>(EcommerceFixture.JsonOptions, ct))!;
            body.Total.Should().Be(3);
            body.Pagina.Should().Be(1);
            body.TamanhoPagina.Should().Be(10);
            body.Pedidos.Select(p => p.Id).Should().Equal(terceiro, segundo, primeiro);

            var maisRecente = body.Pedidos[0];
            maisRecente.Status.Should().Be(StatusPedido.AguardandoPagamento);
            maisRecente.QuantidadePecas.Should().Be(4);
            maisRecente.Total.Should().Be(4 * 59.90m);
            maisRecente.CriadoEm.Should().Be(Dia(3));
        }

        [Fact]
        public async Task Should_PaginarETotalizar()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var (eu, _, client) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(ct);

            var maisAntigo = await PrepararPedido(ct, catalogo, eu, StatusPedido.Entregue, Dia(1));
            await PrepararPedido(ct, catalogo, eu, StatusPedido.Entregue, Dia(2));
            await PrepararPedido(ct, catalogo, eu, StatusPedido.Entregue, Dia(3));

            // Act
            var response = await client.GetAsync($"{Endpoint}?pagina=2&tamanhoPagina=2", ct);

            // Assert
            var body = (await response.Content.ReadFromJsonAsync<ResponsePedidos>(EcommerceFixture.JsonOptions, ct))!;
            body.Total.Should().Be(3);
            body.Pedidos.Should().ContainSingle().Which.Id.Should().Be(maisAntigo);
        }

        [Fact]
        public async Task Should_ReturnListaVazia_When_SemPedidos()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var (_, _, client) = await PrepararClientes();

            // Act
            var response = await client.GetAsync(Endpoint, ct);

            // Assert
            var body = (await response.Content.ReadFromJsonAsync<ResponsePedidos>(EcommerceFixture.JsonOptions, ct))!;
            body.Total.Should().Be(0);
            body.Pedidos.Should().BeEmpty();
        }

        [Theory]
        [InlineData("pagina=0", "A página deve ser maior ou igual a 1.")]
        [InlineData("tamanhoPagina=51", "O tamanho da página deve estar entre 1 e 50.")]
        public async Task Should_Return400_When_PaginacaoInvalida(string query, string mensagemEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var (_, _, client) = await PrepararClientes();

            // Act
            var response = await client.GetAsync($"{Endpoint}?{query}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await MensagensDeErro(response, ct)).Should().Equal(mensagemEsperada);
        }

        // ════════════════════════════════════════════════════════════════
        // DETALHE
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ReturnDetalheDoMeuPedido()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var (eu, _, client) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(ct);
            var pedidoId = await PrepararPedido(ct, catalogo, eu, StatusPedido.Pago, Dia(1), quantidade: 2, comBordado: true);

            // Act
            var response = await client.GetAsync($"{Endpoint}/{pedidoId}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = (await response.Content.ReadFromJsonAsync<ResponsePedido>(EcommerceFixture.JsonOptions, ct))!;
            body.Id.Should().Be(pedidoId);
            body.Status.Should().Be(StatusPedido.Pago);
            body.Total.Should().Be(149.80m);

            var item = body.Itens.Should().ContainSingle().Subject;
            item.Produto.Should().Be("Camiseta Polo");
            item.Tamanho.Should().Be("M");
            item.Cor.Should().Be("Azul");
            item.Quantidade.Should().Be(2);
            item.PrecoUnitario.Should().Be(74.90m);
            item.Subtotal.Should().Be(149.80m);
            item.Personalizacoes.Should().ContainSingle().Which.Valor.Should().Be("Bordado");
        }

        // As datas são gravadas em UTC: a resposta precisa do "Z", senão o navegador lê como horário local
        [Fact]
        public async Task Should_DevolverDatasComFusoUtc()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var (eu, _, client) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(ct);
            var pedidoId = await PrepararPedido(ct, catalogo, eu, StatusPedido.AguardandoPagamento, Dia(3));

            // Act
            var detalhe = await client.GetFromJsonAsync<JsonElement>($"{Endpoint}/{pedidoId}", ct);
            var cancelado = await (await client.PostAsync($"{Endpoint}/{pedidoId}/cancelar", null, ct)).Content.ReadFromJsonAsync<JsonElement>(ct);

            // Assert
            detalhe.GetProperty("criadoEm").GetString().Should().Be("2026-10-03T12:00:00Z");
            cancelado.GetProperty("atualizadoEm").GetString().Should().EndWith("Z");
        }

        // O pedido de outro cliente responde igual a um id inexistente: não revela que ele existe
        [Fact]
        public async Task Should_Return404_When_PedidoDeOutroClienteOuInexistente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var (_, outro, client) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(ct);
            var pedidoDoOutro = await PrepararPedido(ct, catalogo, outro, StatusPedido.Pago, Dia(1));

            // Act
            var deOutro = await client.GetAsync($"{Endpoint}/{pedidoDoOutro}", ct);
            var inexistente = await client.GetAsync($"{Endpoint}/999999", ct);

            // Assert
            deOutro.StatusCode.Should().Be(HttpStatusCode.NotFound);
            inexistente.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await MensagensDeErro(deOutro, ct)).Should().Equal("Pedido não encontrado.");
            (await MensagensDeErro(inexistente, ct)).Should().Equal("Pedido não encontrado.");
        }

        // ════════════════════════════════════════════════════════════════
        // CANCELAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Cancelar_When_AguardandoPagamento()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var (eu, _, client) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(ct);
            var pedidoId = await PrepararPedido(ct, catalogo, eu, StatusPedido.AguardandoPagamento, Dia(1));

            // Act
            var response = await client.PostAsync($"{Endpoint}/{pedidoId}/cancelar", null, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = (await response.Content.ReadFromJsonAsync<ResponsePedido>(EcommerceFixture.JsonOptions, ct))!;
            body.Status.Should().Be(StatusPedido.Cancelado);
            body.AtualizadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            (await dbContext.Pedidos.SingleAsync(p => p.Id == pedidoId, ct)).Status.Should().Be(StatusPedido.Cancelado);

            // o estoque não tinha sido baixado, então nada volta
            (await dbContext.Estoque.SingleAsync(e => e.VarianteId == catalogo.VarianteId, ct)).Quantidade.Should().Be(10);
            (await dbContext.EstoqueMovimentacoes.CountAsync(ct)).Should().Be(0);
        }

        [Theory]
        [InlineData(StatusPedido.Pago)]
        [InlineData(StatusPedido.EmProducao)]
        [InlineData(StatusPedido.ProntoParaRetirada)]
        [InlineData(StatusPedido.Entregue)]
        [InlineData(StatusPedido.Cancelado)]
        public async Task Should_Return409EManterStatus_When_PedidoJaSaiuDeAguardandoPagamento(StatusPedido status)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var (eu, _, client) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(ct);
            var pedidoId = await PrepararPedido(ct, catalogo, eu, status, Dia(1));

            // Act
            var response = await client.PostAsync($"{Endpoint}/{pedidoId}/cancelar", null, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await MensagensDeErro(response, ct)).Should().Equal("Só é possível cancelar pedidos que ainda aguardam pagamento. Fale com a loja pelo WhatsApp.");

            using var scope = _fixture.CriarScope();
            var pedido = await _fixture.GetDbContext(scope).Pedidos.SingleAsync(p => p.Id == pedidoId, ct);
            pedido.Status.Should().Be(status);
            pedido.AtualizadoEm.Should().BeNull();
        }

        [Fact]
        public async Task Should_Return404ENaoCancelar_When_PedidoDeOutroCliente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var (_, outro, client) = await PrepararClientes();
            var catalogo = await PrepararCatalogo(ct);
            var pedidoDoOutro = await PrepararPedido(ct, catalogo, outro, StatusPedido.AguardandoPagamento, Dia(1));

            // Act
            var response = await client.PostAsync($"{Endpoint}/{pedidoDoOutro}/cancelar", null, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).Pedidos.SingleAsync(ct)).Status.Should().Be(StatusPedido.AguardandoPagamento);
        }

        // ════════════════════════════════════════════════════════════════
        // SEM LOGIN
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("GET", Endpoint)]
        [InlineData("GET", $"{Endpoint}/1")]
        [InlineData("POST", $"{Endpoint}/1/cancelar")]
        public async Task Should_Return401_When_SemLogin(string metodo, string url)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), url), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        // "eu" está logado (com cookie, como no navegador); o "outro" só existe no banco
        private async Task<(int Eu, int Outro, HttpClient Client)> PrepararClientes()
        {
            var eu = await _fixture.CadastrarCliente("eu@email.com", SENHA);
            var outro = await _fixture.CadastrarCliente("outro@email.com", SENHA);
            var client = await _fixture.CriarClientLogado("eu@email.com", SENHA);

            return (eu.Id, outro.Id, client);
        }

        private Task<Catalogo> PrepararCatalogo(CancellationToken ct) => CenarioPedidos.PrepararCatalogo(_fixture, ct);

        private Task<int> PrepararPedido(CancellationToken ct, Catalogo catalogo, int clienteId, StatusPedido status, DateTime criadoEm, int quantidade = 1, bool comBordado = false) =>
            CenarioPedidos.PrepararPedido(_fixture, ct, catalogo, clienteId, status, criadoEm, quantidade, comBordado);

        private static async Task<List<string?>> MensagensDeErro(HttpResponseMessage response, CancellationToken ct)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            return body.GetProperty("errors").EnumerateArray().Select(erro => erro.GetString()).ToList();
        }
    }
}
