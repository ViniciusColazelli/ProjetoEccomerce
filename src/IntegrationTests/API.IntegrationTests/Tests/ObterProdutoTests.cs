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
    public class ObterProdutoTests : BaseTesteIntegracao
    {
        private const string ENDPOINT = "/api/produto";

        public ObterProdutoTests(EcommerceFixture fixture) : base(fixture) { }

        // ════════════════════════════════════════════════════════════════
        // FLUXO FELIZ
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return200WithDetalhe_When_ProdutoAtivo()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var p = Opcao(TipoOpcao.Tamanho, "P");
            var g = Opcao(TipoOpcao.Tamanho, "G", precoAdicional: 5m);
            var azul = Opcao(TipoOpcao.Cor, "Azul");
            var bordado = Opcao(TipoOpcao.Personalizacao, "Bordado", precoAdicional: 15m);

            var produto = NovoProduto("Camiseta Polo", 59.90m, p, g, azul, bordado);
            produto.Variantes.Add(Variante(p, azul, quantidade: 3));
            produto.Variantes.Add(Variante(g, azul, quantidade: 0));

            var id = await PrepararProduto(ct, produto);

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}/{id}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutoDetalhe>(ct);
            body.Should().NotBeNull();
            body!.Nome.Should().Be("Camiseta Polo");
            body.PrecoBase.Should().Be(59.90m);
            body.Disponivel.Should().BeTrue();

            body.Tamanhos.Select(t => t.Valor).Should().Equal("P", "G");
            body.Tamanhos.Single(t => t.Valor == "G").PrecoAdicional.Should().Be(5m);
            body.Cores.Should().ContainSingle(c => c.Valor == "Azul");
            body.Personalizacoes.Should().ContainSingle(c => c.Valor == "Bordado" && c.PrecoAdicional == 15m);

            body.Variantes.Should().HaveCount(2);
            body.Variantes.Single(v => v.TamanhoId == p.Id).Disponivel.Should().BeTrue();
            body.Variantes.Single(v => v.TamanhoId == g.Id).Disponivel.Should().BeFalse();
        }

        [Fact]
        public async Task Should_ReturnCorNula_When_ProdutoSemCor()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var m = Opcao(TipoOpcao.Tamanho, "M");
            var produto = NovoProduto("Avental", 39.90m, m);
            produto.Variantes.Add(Variante(m, cor: null, quantidade: 2));

            var id = await PrepararProduto(ct, produto);

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}/{id}", ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseProdutoDetalhe>(ct);
            body!.Cores.Should().BeEmpty();
            body.Variantes.Should().ContainSingle(v => v.CorId == null && v.Disponivel);
        }

        // ════════════════════════════════════════════════════════════════
        // REGRA DE NEGÓCIO: disponibilidade
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ReturnIndisponivel_When_NenhumaVarianteTemEstoque()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var m = Opcao(TipoOpcao.Tamanho, "M");
            var produto = NovoProduto("Jaleco", 89.90m, m);
            produto.Variantes.Add(Variante(m, cor: null, quantidade: 0));

            var id = await PrepararProduto(ct, produto);

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}/{id}", ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseProdutoDetalhe>(ct);
            body!.Disponivel.Should().BeFalse();
            body.Variantes.Should().OnlyContain(v => v.Disponivel == false);
        }

        [Fact]
        public async Task Should_ReturnIndisponivel_When_ProdutoSemVariantes()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var id = await PrepararProduto(ct, NovoProduto("Jaleco", 89.90m));

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}/{id}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutoDetalhe>(ct);
            body!.Disponivel.Should().BeFalse();
            body.Variantes.Should().BeEmpty();
        }

        [Fact]
        public async Task Should_NotExporQuantidadeDoEstoque()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var m = Opcao(TipoOpcao.Tamanho, "M");
            var produto = NovoProduto("Jaleco", 89.90m, m);
            produto.Variantes.Add(Variante(m, cor: null, quantidade: 7));

            var id = await PrepararProduto(ct, produto);

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}/{id}", ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var variante = body.GetProperty("variantes")[0];

            variante.TryGetProperty("quantidade", out _).Should().BeFalse(because: "o público só vê se há estoque");
        }

        // ════════════════════════════════════════════════════════════════
        // REGRA DE NEGÓCIO: só opções e variantes ativas
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_NotReturnOpcoesEVariantesInativas()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var m = Opcao(TipoOpcao.Tamanho, "M");
            var gg = Opcao(TipoOpcao.Tamanho, "GG", ativo: false);
            var azul = Opcao(TipoOpcao.Cor, "Azul");
            var verde = Opcao(TipoOpcao.Cor, "Verde", ativo: false);
            var bolso = Opcao(TipoOpcao.Personalizacao, "Bolso extra", ativo: false);

            var produto = NovoProduto("Camiseta Polo", 59.90m, m, gg, azul, verde, bolso);
            produto.Variantes.Add(Variante(m, azul, quantidade: 1));
            produto.Variantes.Add(Variante(m, verde, quantidade: 1));                    // cor inativa
            produto.Variantes.Add(Variante(gg, azul, quantidade: 1));                    // tamanho inativo
            produto.Variantes.Add(Variante(m, cor: null, quantidade: 1, ativo: false)); // variante inativa

            var id = await PrepararProduto(ct, produto);

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}/{id}", ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseProdutoDetalhe>(ct);
            body!.Tamanhos.Select(t => t.Valor).Should().Equal("M");
            body.Cores.Select(c => c.Valor).Should().Equal("Azul");
            body.Personalizacoes.Should().BeEmpty();
            body.Variantes.Should().ContainSingle(v => v.TamanhoId == m.Id && v.CorId == azul.Id);
        }

        // ════════════════════════════════════════════════════════════════
        // NÃO ENCONTRADO
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return404_When_ProdutoInativo()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            var id = await PrepararProduto(ct, NovoProduto("Avental", 39.90m, ativo: false));

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}/{id}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Produto não encontrado.");
        }

        [Fact]
        public async Task Should_Return404_When_ProdutoNaoExiste()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            // Act
            var response = await _fixture.Client.GetAsync($"{ENDPOINT}/999999", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        private async Task<int> PrepararProduto(CancellationToken ct, Produtos produto)
        {
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            await dbContext.Produtos.AddAsync(produto, ct);
            await dbContext.SaveChangesAsync(ct);

            return produto.Id;
        }

        private static Produtos NovoProduto(string nome, decimal preco, params ProdutoOpcoes[] opcoes) =>
            NovoProduto(nome, preco, ativo: true, opcoes);

        private static Produtos NovoProduto(string nome, decimal preco, bool ativo, params ProdutoOpcoes[] opcoes)
        {
            var produto = new Produtos
            {
                Nome = nome,
                Descricao = $"Descrição de {nome}",
                PrecoBase = preco,
                Ativo = ativo
            };

            foreach (var opcao in opcoes)
                produto.Opcoes.Add(opcao);

            return produto;
        }

        private static ProdutoOpcoes Opcao(TipoOpcao tipo, string valor, decimal precoAdicional = 0, bool ativo = true) => new()
        {
            Tipo = tipo,
            Valor = valor,
            PrecoAdicional = precoAdicional,
            Ativo = ativo
        };

        private static ProdutoVariantes Variante(ProdutoOpcoes tamanho, ProdutoOpcoes? cor, int quantidade, bool ativo = true) => new()
        {
            TamanhoOpcao = tamanho,
            CorOpcao = cor,
            Ativo = ativo,
            Estoque = new Estoque { Quantidade = quantidade }
        };
    }
}
