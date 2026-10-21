using API.IntegrationTests.Fixture;
using Application.Request;
using Application.Response;
using Domain.Entities;
using Domain.Enums;
using Domain.Security.Perfis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    public class AdminProdutoTests : BaseTesteIntegracao
    {
        private const string ENDPOINT = "/api/admin/produto";

        public AdminProdutoTests(EcommerceFixture fixture) : base(fixture) { }

        // ════════════════════════════════════════════════════════════════
        // AUTORIZAÇÃO
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return401_When_SemLogin()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;

            // Act
            var response = await _fixture.Client.GetAsync(ENDPOINT, ct);

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
            var response = await client.PostAsJsonAsync(ENDPOINT, NovoRequest("Jaleco", 89.90m), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Você não tem permissão para realizar esta ação.");
        }

        // ════════════════════════════════════════════════════════════════
        // LISTAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ListarInclusiveInativos()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            await PrepararProdutos(ct, NovoProduto("Camiseta Polo", 59.90m), NovoProduto("Avental", 39.90m, ativo: false));

            // Act
            var response = await client.GetAsync(ENDPOINT, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutosAdmin>(ct);
            body!.Total.Should().Be(2);
            body.Produtos.Should().ContainSingle(p => p.Nome == "Avental" && p.Ativo == false);
        }

        [Theory]
        [InlineData("ativo=true", "Camiseta Polo")]
        [InlineData("ativo=false", "Avental")]
        public async Task Should_FiltrarPorStatus(string query, string nomeEsperado)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            await PrepararProdutos(ct, NovoProduto("Camiseta Polo", 59.90m), NovoProduto("Avental", 39.90m, ativo: false));

            // Act
            var response = await client.GetAsync($"{ENDPOINT}?{query}", ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseProdutosAdmin>(ct);
            body!.Produtos.Select(p => p.Nome).Should().Equal(nomeEsperado);
        }

        // ════════════════════════════════════════════════════════════════
        // OBTER
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_ReturnDetalheComQuantidadeEInativos()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            var m = new ProdutoOpcoes { Tipo = TipoOpcao.Tamanho, Valor = "M" };
            var gg = new ProdutoOpcoes { Tipo = TipoOpcao.Tamanho, Valor = "GG", Ativo = false };

            var produto = NovoProduto("Camiseta Polo", 59.90m, ativo: false);
            produto.Opcoes.Add(m);
            produto.Opcoes.Add(gg);
            produto.Variantes.Add(new ProdutoVariantes { TamanhoOpcao = m, Estoque = new Estoque { Quantidade = 7 } });

            await PrepararProdutos(ct, produto);

            // Act
            var response = await client.GetAsync($"{ENDPOINT}/{produto.Id}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutoAdmin>(EcommerceFixture.JsonOptions, ct);
            body!.Ativo.Should().BeFalse();
            body.Opcoes.Should().HaveCount(2);
            body.Opcoes.Should().ContainSingle(o => o.Valor == "GG" && o.Ativo == false && o.Tipo == TipoOpcao.Tamanho);
            body.Variantes.Should().ContainSingle(v => v.Tamanho == "M" && v.Quantidade == 7);
        }

        [Fact]
        public async Task Should_Return404_When_ObterProdutoInexistente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.GetAsync($"{ENDPOINT}/999999", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════
        // REGISTRAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return201AndPersistirAtivo_When_ProdutoValido()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.PostAsJsonAsync(ENDPOINT, NovoRequest("Jaleco", 89.90m), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutoRegistrado>(ct);
            body!.Nome.Should().Be("Jaleco");

            using var scope = _fixture.CriarScope();
            var produto = await _fixture.GetDbContext(scope).Produtos.SingleAsync(p => p.Id == body.Id, ct);

            produto.PrecoBase.Should().Be(89.90m);
            produto.Ativo.Should().BeTrue();
        }

        [Theory]
        [InlineData("", "10", "Nome não pode ser vazio")]
        [InlineData("Jaleco", "0", "O preço base deve ser maior que zero.")]
        [InlineData("Jaleco", "10.999", "O preço deve ter no máximo 8 dígitos inteiros e 2 casas decimais.")]
        public async Task Should_Return400_When_ProdutoInvalido(string nome, string preco, string mensagemEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            var request = NovoRequest(nome, decimal.Parse(preco, CultureInfo.InvariantCulture));

            // Act
            var response = await client.PostAsJsonAsync(ENDPOINT, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors").EnumerateArray().Select(e => e.GetString()).Should().Contain(mensagemEsperada);
        }

        // ════════════════════════════════════════════════════════════════
        // ATUALIZAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return204AndAtualizar_When_ProdutoExiste()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            var produto = NovoProduto("Jaleco", 89.90m);
            await PrepararProdutos(ct, produto);

            var request = new RequestUpdateProduto { Nome = "Jaleco Premium", Descricao = "Nova descrição", PrecoBase = 99.90m };

            // Act
            var response = await client.PutAsJsonAsync($"{ENDPOINT}/{produto.Id}", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = _fixture.CriarScope();
            var salvo = await _fixture.GetDbContext(scope).Produtos.SingleAsync(ct);

            salvo.Nome.Should().Be("Jaleco Premium");
            salvo.Descricao.Should().Be("Nova descrição");
            salvo.PrecoBase.Should().Be(99.90m);
        }

        [Fact]
        public async Task Should_ManterPrecoDosPedidosAntigos_When_PrecoAlterado()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            var produto = NovoProduto("Jaleco", 89.90m);
            await PrepararProdutos(ct, produto);

            // pedidos ainda não têm entidade no EF: o pedido antigo é gravado direto no banco
            using (var scope = _fixture.CriarScope())
            {
                var dbContext = _fixture.GetDbContext(scope);
                var clienteId = await dbContext.Clientes.Select(c => c.Id).FirstAsync(ct);

                await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                    WITH pedido AS (
                        INSERT INTO pedidos (cliente_id, status, total) VALUES ({clienteId}, 'Pago', 89.90) RETURNING id)
                    INSERT INTO pedido_itens (pedido_id, produto_id, quantidade, preco_unitario)
                    SELECT id, {produto.Id}, 1, 89.90 FROM pedido", ct);
            }

            var request = new RequestUpdateProduto { Nome = "Jaleco", PrecoBase = 119.90m };

            // Act
            await client.PutAsJsonAsync($"{ENDPOINT}/{produto.Id}", request, ct);

            // Assert
            using var scopeLeitura = _fixture.CriarScope();
            var precoNoPedido = await _fixture.GetDbContext(scopeLeitura).Database
                .SqlQuery<decimal>($"SELECT preco_unitario AS \"Value\" FROM pedido_itens")
                .SingleAsync(ct);

            precoNoPedido.Should().Be(89.90m);
        }

        [Fact]
        public async Task Should_Return404_When_AtualizarProdutoInexistente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            var request = new RequestUpdateProduto { Nome = "Jaleco", PrecoBase = 10m };

            // Act
            var response = await client.PutAsJsonAsync($"{ENDPOINT}/999999", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Produto não encontrado.");
        }

        // ════════════════════════════════════════════════════════════════
        // ATIVAR E DESATIVAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_SumirDaVitrine_When_ProdutoDesativado()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            var produto = NovoProduto("Jaleco", 89.90m);
            await PrepararProdutos(ct, produto);

            // Act
            var response = await client.PatchAsJsonAsync($"{ENDPOINT}/{produto.Id}/status", new RequestAlterarStatus { Ativo = false }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var vitrine = await _fixture.Client.GetAsync($"/api/produto/{produto.Id}", ct);
            vitrine.StatusCode.Should().Be(HttpStatusCode.NotFound);

            using var scope = _fixture.CriarScope();
            var salvo = await _fixture.GetDbContext(scope).Produtos.SingleAsync(ct);
            salvo.Ativo.Should().BeFalse(because: "o produto é desativado, nunca excluído");
        }

        [Fact]
        public async Task Should_VoltarParaVitrine_When_ProdutoReativado()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            var produto = NovoProduto("Jaleco", 89.90m, ativo: false);
            await PrepararProdutos(ct, produto);

            // Act
            var response = await client.PatchAsJsonAsync($"{ENDPOINT}/{produto.Id}/status", new RequestAlterarStatus { Ativo = true }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var vitrine = await _fixture.Client.GetAsync($"/api/produto/{produto.Id}", ct);
            vitrine.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

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

        private static RequestRegistrarProduto NovoRequest(string nome, decimal preco) => new()
        {
            Nome = nome,
            Descricao = $"Descrição de {nome}",
            PrecoBase = preco
        };
    }
}
