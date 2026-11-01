using API.IntegrationTests.Fixture;
using Application.Response;
using Domain.Entities;
using Domain.Enums;
using Domain.Security.Perfis;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    public class AdminEstoqueTests : BaseTesteIntegracao
    {
        public AdminEstoqueTests(EcommerceFixture fixture) : base(fixture) { }

        private const string Endpoint = "/api/admin/estoque";

        // ════════════════════════════════════════════════════════════════
        // AUTORIZAÇÃO
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return401_When_SemLogin()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            // Act
            var response = await _fixture.Client.GetAsync(Endpoint, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_Return403_When_PerfilCliente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);

            // Act
            var response = await client.GetAsync(Endpoint, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════
        // LISTAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ListarQuantidadeExataPorVariante()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var comEstoque = await PrepararVariante(ct, "Jaleco", "M", quantidade: 7);
            var semLinhaDeEstoque = await PrepararVariante(ct, "Avental", "G", quantidade: null);

            // Act
            var response = await client.GetAsync(Endpoint, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseEstoque>(ct);
            body!.Total.Should().Be(2);
            body.Variantes.Select(v => $"{v.Produto}/{v.Tamanho}/{v.Quantidade}")
                .Should().Equal("Avental/G/0", "Jaleco/M/7");
            body.Variantes.Select(v => v.VarianteId).Should().BeEquivalentTo(new[] { comEstoque.Id, semLinhaDeEstoque.Id });
        }

        [Fact]
        public async Task Should_FiltrarPorProduto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var jaleco = await PrepararVariante(ct, "Jaleco", "M", quantidade: 3);
            await PrepararVariante(ct, "Avental", "G", quantidade: 5);

            // Act
            var body = await client.GetFromJsonAsync<ResponseEstoque>($"{Endpoint}?produtoId={jaleco.ProdutoId}", ct);

            // Assert
            body!.Total.Should().Be(1);
            body.Variantes.Should().ContainSingle(v => v.VarianteId == jaleco.Id && v.Quantidade == 3);
        }

        [Fact]
        public async Task Should_RetornarSomenteZerados_When_SomenteZerados()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            await PrepararVariante(ct, "Jaleco", "M", quantidade: 4);
            var zerada = await PrepararVariante(ct, "Avental", "G", quantidade: 0);
            var semLinhaDeEstoque = await PrepararVariante(ct, "Camiseta", "P", quantidade: null);

            // Act
            var body = await client.GetFromJsonAsync<ResponseEstoque>($"{Endpoint}?somenteZerados=true", ct);

            // Assert
            body!.Total.Should().Be(2);
            body.Variantes.Select(v => v.VarianteId).Should().BeEquivalentTo(new[] { zerada.Id, semLinhaDeEstoque.Id });
        }

        [Fact]
        public async Task Should_Return400_When_PaginaInvalida()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.GetAsync($"{Endpoint}?pagina=0", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ════════════════════════════════════════════════════════════════
        // OBTER
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ReturnEstoqueDaVariante()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M", quantidade: 12, cor: "Branco");

            // Act
            var response = await client.GetAsync($"{Endpoint}/{variante.Id}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseEstoqueVariante>(ct);
            body!.VarianteId.Should().Be(variante.Id);
            body.ProdutoId.Should().Be(variante.ProdutoId);
            body.Produto.Should().Be("Jaleco");
            body.Tamanho.Should().Be("M");
            body.Cor.Should().Be("Branco");
            body.Quantidade.Should().Be(12);
        }

        [Fact]
        public async Task Should_Return404_When_VarianteInexistente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.GetAsync($"{Endpoint}/999999", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Variante não encontrada.");
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        // quantidade nula = variante sem linha na tabela estoque (como as criadas pelo script SQL)
        private async Task<ProdutoVariantes> PrepararVariante(CancellationToken ct, string nomeProduto, string tamanho, int? quantidade, string? cor = null)
        {
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            var produto = new Produtos { Nome = nomeProduto, PrecoBase = 59.90m, Ativo = true };

            var opcaoTamanho = new ProdutoOpcoes { Tipo = TipoOpcao.Tamanho, Valor = tamanho, Ativo = true };
            produto.Opcoes.Add(opcaoTamanho);

            ProdutoOpcoes? opcaoCor = null;
            if (cor is not null)
            {
                opcaoCor = new ProdutoOpcoes { Tipo = TipoOpcao.Cor, Valor = cor, Ativo = true };
                produto.Opcoes.Add(opcaoCor);
            }

            var variante = new ProdutoVariantes
            {
                TamanhoOpcao = opcaoTamanho,
                CorOpcao = opcaoCor,
                Ativo = true,
                Estoque = quantidade.HasValue ? new Estoque { Quantidade = quantidade.Value } : null
            };
            produto.Variantes.Add(variante);

            await dbContext.Produtos.AddAsync(produto, ct);
            await dbContext.SaveChangesAsync(ct);

            return variante;
        }
    }
}
