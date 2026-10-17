using API.IntegrationTests.Fixture;
using Application.Response;
using Domain.Entities;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    public class ListarProdutosTests : BaseTesteIntegracao
    {
        private const string ENDPOINT = "/api/produto";

        public ListarProdutosTests(EcommerceFixture fixture) : base(fixture) { }

        // ════════════════════════════════════════════════════════════════
        // FLUXO FELIZ
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return200WithProdutos_When_ExistemProdutosAtivos()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            await PrepararProdutos(ct,
                NovoProduto("Camiseta Polo", 59.90m),
                NovoProduto("Jaleco", 89.90m));

            // Act
            var response = await _fixture.Client.GetAsync(ENDPOINT, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutos>(ct);
            body.Should().NotBeNull();
            body!.Total.Should().Be(2);
            body.Produtos.Select(p => p.Nome).Should().Equal("Camiseta Polo", "Jaleco");
            body.Produtos.First().PrecoBase.Should().Be(59.90m);
        }

        [Fact]
        public async Task Should_UsePaginacaoPadrao_When_ParametrosNaoInformados()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            await PrepararProdutos(ct, NovoProduto("Camiseta Polo", 59.90m));

            // Act
            var response = await _fixture.Client.GetAsync(ENDPOINT, ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseProdutos>(ct);
            body!.Pagina.Should().Be(1);
            body.TamanhoPagina.Should().Be(20);
        }

        [Fact]
        public async Task Should_Return200WithListaVazia_When_NaoExistemProdutos()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            await PrepararProdutos(ct);

            // Act
            var response = await _fixture.Client.GetAsync(ENDPOINT, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutos>(ct);
            body!.Produtos.Should().BeEmpty();
            body.Total.Should().Be(0);
        }

        // ════════════════════════════════════════════════════════════════
        // REGRA DE NEGÓCIO: só produtos ativos
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_NotReturnProdutoInativo()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            await PrepararProdutos(ct,
                NovoProduto("Camiseta Polo", 59.90m),
                NovoProduto("Avental", 39.90m, ativo: false));

            // Act
            var response = await _fixture.Client.GetAsync(ENDPOINT, ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseProdutos>(ct);
            body!.Total.Should().Be(1);
            body.Produtos.Should().ContainSingle(p => p.Nome == "Camiseta Polo");
            body.Produtos.Should().NotContain(p => p.Nome == "Avental");
        }

        // ════════════════════════════════════════════════════════════════
        // PAGINAÇÃO
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ReturnSegundaPagina_When_PaginaDoisSolicitada()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var produtos = Enumerable.Range(1, 25)
                .Select(i => NovoProduto($"Produto {i:D2}", 10m))
                .ToArray();

            await PrepararProdutos(ct, produtos);

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}?pagina=2&tamanhoPagina=20", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutos>(ct);
            body!.Pagina.Should().Be(2);
            body.Total.Should().Be(25);
            body.Produtos.Should().HaveCount(5);
            body.Produtos.First().Nome.Should().Be("Produto 21");
        }

        // ════════════════════════════════════════════════════════════════
        // VALIDAÇÃO: parâmetros de paginação inválidos
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("pagina=0")]
        [InlineData("pagina=-1")]
        [InlineData("tamanhoPagina=0")]
        [InlineData("tamanhoPagina=51")]
        public async Task Should_Return400_When_PaginacaoInvalida(string query)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}?{query}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // ResponseErro não tem construtor sem parâmetros, então lemos o JSON direto
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors").GetArrayLength().Should().BeGreaterThan(0);
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        // O banco já chega limpo em cada teste (BaseTesteIntegracao)
        private async Task PrepararProdutos(CancellationToken ct, params Produtos[] produtos)
        {
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            await dbContext.Produtos.AddRangeAsync(produtos, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        private static Produtos NovoProduto(string nome, decimal preco, bool ativo = true) => new()
        {
            Nome = nome,
            Descricao = $"Descrição de {nome}",
            PrecoBase = preco,
            Ativo = ativo
        };
    }
}
