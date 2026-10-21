using API.IntegrationTests.Fixture;
using Application.Response;
using Domain.Entities;
using Domain.Enums;
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
        // BUSCA POR NOME
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_FiltrarPorNome_IgnorandoMaiusculas()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            await PrepararProdutos(ct,
                NovoProduto("Camiseta Polo", 59.90m),
                NovoProduto("Polo Infantil", 49.90m),
                NovoProduto("Jaleco", 89.90m));

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}?nome=POLO", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutos>(ct);
            body!.Total.Should().Be(2);
            body.Produtos.Select(p => p.Nome).Should().Equal("Camiseta Polo", "Polo Infantil");
        }

        [Fact]
        public async Task Should_Return200WithListaVazia_When_NomeNaoEncontrado()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            await PrepararProdutos(ct, NovoProduto("Jaleco", 89.90m));

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}?nome=avental", ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseProdutos>(ct);
            body!.Total.Should().Be(0);
            body.Produtos.Should().BeEmpty();
        }

        [Fact]
        public async Task Should_ContarTotalComFiltro_When_BuscaPaginada()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var polos = Enumerable.Range(1, 15).Select(i => NovoProduto($"Polo {i:D2}", 10m));
            var jalecos = Enumerable.Range(1, 5).Select(i => NovoProduto($"Jaleco {i:D2}", 10m));

            await PrepararProdutos(ct, polos.Concat(jalecos).ToArray());

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}?nome=polo&tamanhoPagina=10", ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseProdutos>(ct);
            body!.Total.Should().Be(15, because: "o total considera o filtro, não a tabela inteira");
            body.Produtos.Should().HaveCount(10);
        }

        // ════════════════════════════════════════════════════════════════
        // ORDENAÇÃO
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("", "Avental", "Camiseta", "Jaleco")]
        [InlineData("ordenarPor=nome&direcao=desc", "Jaleco", "Camiseta", "Avental")]
        [InlineData("ordenarPor=preco", "Camiseta", "Jaleco", "Avental")]
        [InlineData("ordenarPor=preco&direcao=desc", "Avental", "Jaleco", "Camiseta")]
        [InlineData("ordenarPor=PRECO&direcao=DESC", "Avental", "Jaleco", "Camiseta")]
        public async Task Should_OrdenarProdutos(string query, params string[] nomesEsperados)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            // preços escolhidos para que cada ordenação dê um resultado diferente
            await PrepararProdutos(ct,
                NovoProduto("Avental", 89.90m),
                NovoProduto("Camiseta", 39.90m),
                NovoProduto("Jaleco", 59.90m));

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}?{query}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutos>(ct);
            body!.Produtos.Select(p => p.Nome).Should().Equal(nomesEsperados);
        }

        // ════════════════════════════════════════════════════════════════
        // FILTRO: somente disponíveis
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_RetornarSomenteComEstoque_When_SomenteDisponiveis()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            await PrepararProdutos(ct,
                ComVariante(NovoProduto("Com estoque", 10m), quantidade: 5),
                ComVariante(NovoProduto("Sem estoque", 10m), quantidade: 0),
                ComVariante(NovoProduto("Variante inativa", 10m), quantidade: 5, varianteAtiva: false),
                ComVariante(NovoProduto("Tamanho inativo", 10m), quantidade: 5, tamanhoAtivo: false),
                NovoProduto("Sem variantes", 10m));

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}?somenteDisponiveis=true", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutos>(ct);
            body!.Total.Should().Be(1);
            body.Produtos.Should().ContainSingle(p => p.Nome == "Com estoque");
        }

        [Fact]
        public async Task Should_RetornarTodosAtivos_When_SomenteDisponiveisNaoInformado()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            await PrepararProdutos(ct,
                ComVariante(NovoProduto("Com estoque", 10m), quantidade: 5),
                ComVariante(NovoProduto("Sem estoque", 10m), quantidade: 0));

            // Act
            var response = await _fixture.Client.GetAsync(ENDPOINT, ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseProdutos>(ct);
            body!.Total.Should().Be(2);
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
        // VALIDAÇÃO: busca e ordenação inválidas
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("ordenarPor=categoria", "Ordenação inválida. Use nome ou preco.")]
        [InlineData("direcao=cima", "Direção inválida. Use asc ou desc.")]
        public async Task Should_Return400_When_OrdenacaoInvalida(string query, string mensagemEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}?{query}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be(mensagemEsperada);
        }

        [Fact]
        public async Task Should_Return400_When_BuscaPorNomeMuitoLonga()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var nome = new string('a', 201);

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}?nome={nome}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("A busca por nome deve ter no máximo 200 caracteres.");
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

        // Adiciona ao produto o tamanho M e a variante M (sem cor) com o estoque informado
        private static Produtos ComVariante(Produtos produto, int quantidade, bool varianteAtiva = true, bool tamanhoAtivo = true)
        {
            var tamanho = new ProdutoOpcoes { Tipo = TipoOpcao.Tamanho, Valor = "M", Ativo = tamanhoAtivo };

            produto.Opcoes.Add(tamanho);
            produto.Variantes.Add(new ProdutoVariantes
            {
                TamanhoOpcao = tamanho,
                Ativo = varianteAtiva,
                Estoque = new Estoque { Quantidade = quantidade }
            });

            return produto;
        }
    }
}
