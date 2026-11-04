using API.IntegrationTests.Fixture;
using Application.Request;
using Application.Response;
using Domain.Enums;
using Domain.Security.Perfis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using static API.IntegrationTests.Fixture.CenarioPedidos;

namespace API.IntegrationTests.Tests
{
    public class AdminStatusPedidoTests : BaseTesteIntegracao
    {
        public AdminStatusPedidoTests(EcommerceFixture fixture) : base(fixture) { }

        private const string Endpoint = "/api/admin/pedidos";

        // ════════════════════════════════════════════════════════════════
        // AUTORIZAÇÃO
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return401_When_SemLogin()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/1/status", Status(StatusPedido.Pago), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // O cliente não pode marcar o próprio pedido como pago
        [Fact]
        public async Task Should_Return403EManterStatus_When_PerfilCliente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(_fixture, ct);
            var pedidoId = await PrepararPedido(_fixture, ct, catalogo, await ClienteId(ct), StatusPedido.AguardandoPagamento, Dia(1));

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(StatusPedido.Pago), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await StatusNoBanco(pedidoId, ct)).Should().Be(StatusPedido.AguardandoPagamento);
            await EstoqueDeveSer(catalogo.VarianteId, 10, ct);
        }

        // ════════════════════════════════════════════════════════════════
        // CICLO DE VIDA
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(StatusPedido.AguardandoPagamento, StatusPedido.Pago)]
        [InlineData(StatusPedido.Pago, StatusPedido.EmProducao)]
        [InlineData(StatusPedido.Pago, StatusPedido.ProntoParaRetirada)]   // peça sem personalização pula a produção
        [InlineData(StatusPedido.EmProducao, StatusPedido.ProntoParaRetirada)]
        [InlineData(StatusPedido.ProntoParaRetirada, StatusPedido.Entregue)]
        public async Task Should_AvancarStatus_When_TransicaoPermitida(StatusPedido atual, StatusPedido novo)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct);
            var pedidoId = await PrepararPedido(_fixture, ct, catalogo, await ClienteId(ct), atual, Dia(1));

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(novo), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = (await response.Content.ReadFromJsonAsync<ResponsePedidoAdmin>(EcommerceFixture.JsonOptions, ct))!;
            body.Status.Should().Be(novo);
            body.AtualizadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
            body.Cliente.Email.Should().NotBeEmpty();

            (await StatusNoBanco(pedidoId, ct)).Should().Be(novo);
        }

        [Theory]
        [InlineData(StatusPedido.AguardandoPagamento, StatusPedido.EmProducao)]   // sem pagamento não produz
        [InlineData(StatusPedido.AguardandoPagamento, StatusPedido.Entregue)]
        [InlineData(StatusPedido.AguardandoPagamento, StatusPedido.AguardandoPagamento)]
        [InlineData(StatusPedido.Pago, StatusPedido.AguardandoPagamento)]         // não volta etapa
        [InlineData(StatusPedido.Pago, StatusPedido.Pago)]
        [InlineData(StatusPedido.EmProducao, StatusPedido.Pago)]
        [InlineData(StatusPedido.ProntoParaRetirada, StatusPedido.EmProducao)]
        [InlineData(StatusPedido.Pago, StatusPedido.Cancelado)]                   // pago não se cancela
        [InlineData(StatusPedido.EmProducao, StatusPedido.Cancelado)]
        [InlineData(StatusPedido.ProntoParaRetirada, StatusPedido.Cancelado)]
        [InlineData(StatusPedido.Entregue, StatusPedido.Cancelado)]               // finais
        [InlineData(StatusPedido.Cancelado, StatusPedido.Pago)]
        public async Task Should_Return409SemAlterarNada_When_TransicaoNaoPermitida(StatusPedido atual, StatusPedido novo)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct);
            var pedidoId = await PrepararPedido(_fixture, ct, catalogo, await ClienteId(ct), atual, Dia(1));

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(novo), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await MensagensDeErro(response, ct)).Should().Equal($"Não é possível mudar o pedido de {atual} para {novo}.");

            (await StatusNoBanco(pedidoId, ct)).Should().Be(atual);
            await EstoqueDeveSer(catalogo.VarianteId, 10, ct);
            await MovimentacoesDevemSer(0, ct);
        }

        [Theory]
        [InlineData("Pendente")]
        [InlineData("")]
        public async Task Should_Return400_When_StatusInvalido(string status)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/1/status", new RequestAlterarStatusPedido { Status = status }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await MensagensDeErro(response, ct)).Should().Equal("Status inválido. Use AguardandoPagamento, Pago, EmProducao, ProntoParaRetirada, Entregue ou Cancelado.");
        }

        [Fact]
        public async Task Should_AceitarStatusSemDiferenciarMaiusculas()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct);
            var pedidoId = await PrepararPedido(_fixture, ct, catalogo, await ClienteId(ct), StatusPedido.AguardandoPagamento, Dia(1));

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", new RequestAlterarStatusPedido { Status = " pago " }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await StatusNoBanco(pedidoId, ct)).Should().Be(StatusPedido.Pago);
        }

        [Fact]
        public async Task Should_Return404_When_PedidoInexistente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/999999/status", Status(StatusPedido.Pago), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await MensagensDeErro(response, ct)).Should().Equal("Pedido não encontrado.");
        }

        // ════════════════════════════════════════════════════════════════
        // PAGAMENTO: BAIXA DO ESTOQUE
        // ════════════════════════════════════════════════════════════════

        // 2 M com bordado + 1 M sem bordado + 4 G: a variante M sai uma vez só, com 3
        [Fact]
        public async Task Should_BaixarEstoquePorVarianteLigadoAoPedido_When_Pago()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct, estoque: 10, estoqueG: 4);
            var pedidoId = await PrepararPedidoComItens(_fixture, ct, catalogo, await ClienteId(ct), StatusPedido.AguardandoPagamento, Dia(1), null,
                (catalogo.VarianteId, 2, true), (catalogo.VarianteId, 1, false), (catalogo.VarianteGId, 4, false));

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(StatusPedido.Pago), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            await EstoqueDeveSer(catalogo.VarianteId, 7, ct);
            await EstoqueDeveSer(catalogo.VarianteGId, 0, ct);

            using var scope = _fixture.CriarScope();
            var movimentacoes = await _fixture.GetDbContext(scope).EstoqueMovimentacoes.OrderBy(m => m.VarianteId).ToListAsync(ct);
            movimentacoes.Select(m => (m.VarianteId, m.Tipo, m.Quantidade, m.PedidoId, m.Motivo)).Should().Equal(
                (catalogo.VarianteId, TipoMovimentacao.Saida, 3, pedidoId, $"Pedido #{pedidoId} pago"),
                (catalogo.VarianteGId, TipoMovimentacao.Saida, 4, pedidoId, $"Pedido #{pedidoId} pago"));
        }

        // O histórico do estoque já filtrava por pedidoId: agora as baixas aparecem lá
        [Fact]
        public async Task Should_AparecerNoHistoricoDoEstoque_When_FiltradoPeloPedido()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct);
            var pedidoId = await PrepararPedido(_fixture, ct, catalogo, await ClienteId(ct), StatusPedido.AguardandoPagamento, Dia(1), quantidade: 2);
            await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(StatusPedido.Pago), ct);

            // Act
            var historico = await client.GetFromJsonAsync<ResponseMovimentacoes>($"/api/admin/estoque/movimentacoes?pedidoId={pedidoId}", EcommerceFixture.JsonOptions, ct);

            // Assert
            var movimentacao = historico!.Movimentacoes.Should().ContainSingle().Subject;
            movimentacao.PedidoId.Should().Be(pedidoId);
            movimentacao.Tipo.Should().Be(TipoMovimentacao.Saida);
            movimentacao.Quantidade.Should().Be(2);
        }

        // Nada é baixado pela metade: se uma peça falta, as outras também ficam como estavam
        [Fact]
        public async Task Should_Return409SemBaixarNada_When_AlgumaPecaSemEstoque()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct, estoque: 10, estoqueG: 1);
            var pedidoId = await PrepararPedidoComItens(_fixture, ct, catalogo, await ClienteId(ct), StatusPedido.AguardandoPagamento, Dia(1), null,
                (catalogo.VarianteId, 2, false), (catalogo.VarianteGId, 3, false));

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(StatusPedido.Pago), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await MensagensDeErro(response, ct)).Should().Equal("Não há estoque suficiente de Camiseta Polo (G, Azul).");

            (await StatusNoBanco(pedidoId, ct)).Should().Be(StatusPedido.AguardandoPagamento);
            await EstoqueDeveSer(catalogo.VarianteId, 10, ct);
            await EstoqueDeveSer(catalogo.VarianteGId, 1, ct);
            await MovimentacoesDevemSer(0, ct);
        }

        [Fact]
        public async Task Should_ListarTodasAsPecasSemEstoque_When_Pago()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct, estoque: 0, estoqueG: 0);
            var pedidoId = await PrepararPedidoComItens(_fixture, ct, catalogo, await ClienteId(ct), StatusPedido.AguardandoPagamento, Dia(1), null,
                (catalogo.VarianteId, 1, false), (catalogo.VarianteGId, 1, false));

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(StatusPedido.Pago), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await MensagensDeErro(response, ct)).Should().BeEquivalentTo(
                "Não há estoque suficiente de Camiseta Polo (M, Azul).",
                "Não há estoque suficiente de Camiseta Polo (G, Azul).");
        }

        // Pedido antigo, anterior às variantes: não há estoque para baixar, e o pagamento segue
        [Fact]
        public async Task Should_IgnorarItemSemVariante_When_Pago()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct);
            var pedidoId = await PrepararPedidoComItens(_fixture, ct, catalogo, await ClienteId(ct), StatusPedido.AguardandoPagamento, Dia(1), null,
                (null, 2, false), (catalogo.VarianteId, 1, false));

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(StatusPedido.Pago), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            await EstoqueDeveSer(catalogo.VarianteId, 9, ct);
            await MovimentacoesDevemSer(1, ct);
        }

        // ════════════════════════════════════════════════════════════════
        // CANCELAMENTO PELO ADMIN
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_CancelarSemMexerNoEstoque_When_AindaNaoPago()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct);
            var pedidoId = await PrepararPedido(_fixture, ct, catalogo, await ClienteId(ct), StatusPedido.AguardandoPagamento, Dia(1), quantidade: 3);

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(StatusPedido.Cancelado), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            await EstoqueDeveSer(catalogo.VarianteId, 10, ct);
            await MovimentacoesDevemSer(0, ct);
        }

        // Pago pela API e depois a tentativa de cancelar: recusada, e a baixa do estoque continua valendo
        [Fact]
        public async Task Should_Return409EManterBaixa_When_CancelarDepoisDoPagamento()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct, estoque: 10);
            var pedidoId = await PrepararPedido(_fixture, ct, catalogo, await ClienteId(ct), StatusPedido.AguardandoPagamento, Dia(1), quantidade: 3);

            (await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(StatusPedido.Pago), ct)).StatusCode.Should().Be(HttpStatusCode.OK);

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(StatusPedido.Cancelado), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await MensagensDeErro(response, ct)).Should().Equal("Não é possível mudar o pedido de Pago para Cancelado.");

            (await StatusNoBanco(pedidoId, ct)).Should().Be(StatusPedido.Pago);
            await EstoqueDeveSer(catalogo.VarianteId, 7, ct);
            await MovimentacoesDevemSer(1, ct);
        }

        // ════════════════════════════════════════════════════════════════
        // CONCORRÊNCIA
        // ════════════════════════════════════════════════════════════════

        // Dois pedidos disputando a última peça: só um pagamento é confirmado, em qualquer ordem de atendimento
        [Fact]
        public async Task Should_ConfirmarSoUmPagamento_When_DoisPedidosDisputamAUltimaPeca()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct, estoque: 1);
            var clienteId = await ClienteId(ct);
            var primeiro = await PrepararPedido(_fixture, ct, catalogo, clienteId, StatusPedido.AguardandoPagamento, Dia(1));
            var segundo = await PrepararPedido(_fixture, ct, catalogo, clienteId, StatusPedido.AguardandoPagamento, Dia(2));

            // Act
            var respostas = await Task.WhenAll(
                client.PostAsJsonAsync($"{Endpoint}/{primeiro}/status", Status(StatusPedido.Pago), ct),
                client.PostAsJsonAsync($"{Endpoint}/{segundo}/status", Status(StatusPedido.Pago), ct));

            // Assert
            respostas.Select(r => r.StatusCode).Should().BeEquivalentTo(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict });

            await EstoqueDeveSer(catalogo.VarianteId, 0, ct);
            await MovimentacoesDevemSer(1, ct);

            using var scope = _fixture.CriarScope();
            var status = await _fixture.GetDbContext(scope).Pedidos.Select(p => p.Status).ToListAsync(ct);
            status.Should().BeEquivalentTo(new[] { StatusPedido.Pago, StatusPedido.AguardandoPagamento });
        }

        // Dois admins confirmando o mesmo pedido: o estoque é baixado uma vez só
        [Fact]
        public async Task Should_BaixarEstoqueUmaVezSo_When_MesmoPedidoConfirmadoDuasVezesAoMesmoTempo()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var catalogo = await PrepararCatalogo(_fixture, ct, estoque: 10);
            var pedidoId = await PrepararPedido(_fixture, ct, catalogo, await ClienteId(ct), StatusPedido.AguardandoPagamento, Dia(1), quantidade: 2);

            // Act
            var respostas = await Task.WhenAll(
                client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(StatusPedido.Pago), ct),
                client.PostAsJsonAsync($"{Endpoint}/{pedidoId}/status", Status(StatusPedido.Pago), ct));

            // Assert
            respostas.Select(r => r.StatusCode).Should().BeEquivalentTo(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict });

            await EstoqueDeveSer(catalogo.VarianteId, 8, ct);
            await MovimentacoesDevemSer(1, ct);
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        private static RequestAlterarStatusPedido Status(StatusPedido status) => new() { Status = status.ToString() };

        private async Task<int> ClienteId(CancellationToken ct)
        {
            var cliente = await _fixture.CadastrarCliente($"{Guid.NewGuid():N}@email.com", "Senha@123");

            return cliente.Id;
        }

        private async Task<StatusPedido> StatusNoBanco(int pedidoId, CancellationToken ct)
        {
            using var scope = _fixture.CriarScope();

            return await _fixture.GetDbContext(scope).Pedidos.Where(p => p.Id == pedidoId).Select(p => p.Status).SingleAsync(ct);
        }

        private async Task EstoqueDeveSer(int varianteId, int quantidade, CancellationToken ct)
        {
            using var scope = _fixture.CriarScope();
            var estoque = await _fixture.GetDbContext(scope).Estoque.SingleAsync(e => e.VarianteId == varianteId, ct);

            estoque.Quantidade.Should().Be(quantidade);
        }

        private async Task MovimentacoesDevemSer(int quantidade, CancellationToken ct)
        {
            using var scope = _fixture.CriarScope();

            (await _fixture.GetDbContext(scope).EstoqueMovimentacoes.CountAsync(ct)).Should().Be(quantidade);
        }

        private static async Task<List<string?>> MensagensDeErro(HttpResponseMessage response, CancellationToken ct)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            return body.GetProperty("errors").EnumerateArray().Select(erro => erro.GetString()).ToList();
        }
    }
}
