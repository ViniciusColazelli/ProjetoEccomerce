using API.IntegrationTests.Fixture;
using Application.Request;
using Application.Response;
using Domain.Entities;
using Domain.Enums;
using Domain.Security.Perfis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        // ENTRADA
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_SomarAoSaldoEGravarMovimentacao_When_Entrada()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M", quantidade: 5);
            var request = new RequestMovimentarEstoque { Quantidade = 10, Motivo = "  Reposição  " };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{variante.Id}/entrada", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseEstoqueVariante>(ct);
            body!.Quantidade.Should().Be(15);

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            (await dbContext.Estoque.SingleAsync(ct)).Quantidade.Should().Be(15);

            var movimentacao = await dbContext.EstoqueMovimentacoes.SingleAsync(ct);
            movimentacao.VarianteId.Should().Be(variante.Id);
            movimentacao.Tipo.Should().Be(TipoMovimentacao.Entrada);
            movimentacao.Quantidade.Should().Be(10);
            movimentacao.Motivo.Should().Be("Reposição");
        }

        [Fact]
        public async Task Should_CriarLinhaDeEstoque_When_EntradaEmVarianteSemEstoque()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M", quantidade: null);
            var request = new RequestMovimentarEstoque { Quantidade = 3, Motivo = "Estoque inicial" };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{variante.Id}/entrada", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope = _fixture.CriarScope();
            var estoque = await _fixture.GetDbContext(scope).Estoque.SingleAsync(ct);
            estoque.VarianteId.Should().Be(variante.Id);
            estoque.Quantidade.Should().Be(3);
        }

        [Theory]
        [InlineData(0, "Reposição", "A quantidade deve ser maior que zero.")]
        [InlineData(-1, "Reposição", "A quantidade deve ser maior que zero.")]
        [InlineData(5, "   ", "Informe o motivo da movimentação.")]
        public async Task Should_Return400_When_MovimentacaoInvalida(int quantidade, string motivo, string mensagemEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M", quantidade: 5);
            var request = new RequestMovimentarEstoque { Quantidade = quantidade, Motivo = motivo };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{variante.Id}/entrada", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be(mensagemEsperada);
        }

        [Fact]
        public async Task Should_Return404_When_EntradaEmVarianteInexistente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var request = new RequestMovimentarEstoque { Quantidade = 1, Motivo = "Reposição" };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/999999/entrada", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Should_Return403_When_EntradaComPerfilCliente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var variante = await PrepararVariante(ct, "Jaleco", "M", quantidade: 5);
            var request = new RequestMovimentarEstoque { Quantidade = 1, Motivo = "Reposição" };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{variante.Id}/entrada", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════
        // SAÍDA
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_SubtrairDoSaldoEGravarMovimentacao_When_Saida()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M", quantidade: 5);
            var request = new RequestMovimentarEstoque { Quantidade = 5, Motivo = "Peça danificada" };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{variante.Id}/saida", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseEstoqueVariante>(ct);
            body!.Quantidade.Should().Be(0);

            using var scope = _fixture.CriarScope();
            var movimentacao = await _fixture.GetDbContext(scope).EstoqueMovimentacoes.SingleAsync(ct);
            movimentacao.Tipo.Should().Be(TipoMovimentacao.Saida);
            movimentacao.Quantidade.Should().Be(5);
            movimentacao.Motivo.Should().Be("Peça danificada");
        }

        [Fact]
        public async Task Should_Return409ENaoAlterarNada_When_SaidaMaiorQueSaldo()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M", quantidade: 2);
            var request = new RequestMovimentarEstoque { Quantidade = 3, Motivo = "Perda" };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{variante.Id}/saida", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Estoque insuficiente para realizar esta operação.");

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            (await dbContext.Estoque.SingleAsync(ct)).Quantidade.Should().Be(2);
            (await dbContext.EstoqueMovimentacoes.CountAsync(ct)).Should().Be(0);
        }

        [Fact]
        public async Task Should_Return409_When_SaidaEmVarianteSemEstoque()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M", quantidade: null);
            var request = new RequestMovimentarEstoque { Quantidade = 1, Motivo = "Perda" };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{variante.Id}/saida", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).Estoque.CountAsync(ct)).Should().Be(0);
        }

        // ════════════════════════════════════════════════════════════════
        // AJUSTE DE INVENTÁRIO
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(5, 8, TipoMovimentacao.Entrada, 3)]
        [InlineData(5, 2, TipoMovimentacao.Saida, 3)]
        [InlineData(4, 0, TipoMovimentacao.Saida, 4)]
        public async Task Should_GravarDiferenca_When_Ajuste(int saldoAtual, int novaQuantidade, TipoMovimentacao tipoEsperado, int diferencaEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M", quantidade: saldoAtual);
            var request = new RequestAjustarEstoque { NovaQuantidade = novaQuantidade, Motivo = "Inventário mensal" };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{variante.Id}/ajuste", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseEstoqueVariante>(ct);
            body!.Quantidade.Should().Be(novaQuantidade);

            using var scope = _fixture.CriarScope();
            var movimentacao = await _fixture.GetDbContext(scope).EstoqueMovimentacoes.SingleAsync(ct);
            movimentacao.Tipo.Should().Be(tipoEsperado);
            movimentacao.Quantidade.Should().Be(diferencaEsperada);
            movimentacao.Motivo.Should().Be("Inventário mensal");
        }

        [Fact]
        public async Task Should_CriarLinhaDeEstoque_When_AjusteEmVarianteSemEstoque()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M", quantidade: null);
            var request = new RequestAjustarEstoque { NovaQuantidade = 6, Motivo = "Contagem inicial" };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{variante.Id}/ajuste", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            (await dbContext.Estoque.SingleAsync(ct)).Quantidade.Should().Be(6);
            (await dbContext.EstoqueMovimentacoes.SingleAsync(ct)).Tipo.Should().Be(TipoMovimentacao.Entrada);
        }

        [Theory]
        [InlineData(5, "Inventário", "A quantidade contada é igual ao saldo atual. Não há ajuste a fazer.")]
        [InlineData(-1, "Inventário", "A quantidade contada não pode ser negativa.")]
        [InlineData(3, "", "Informe o motivo da movimentação.")]
        public async Task Should_Return400_When_AjusteInvalido(int novaQuantidade, string motivo, string mensagemEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M", quantidade: 5);
            var request = new RequestAjustarEstoque { NovaQuantidade = novaQuantidade, Motivo = motivo };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/{variante.Id}/ajuste", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be(mensagemEsperada);

            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).EstoqueMovimentacoes.CountAsync(ct)).Should().Be(0);
        }

        [Fact]
        public async Task Should_Return404_When_AjusteEmVarianteInexistente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var request = new RequestAjustarEstoque { NovaQuantidade = 1, Motivo = "Inventário" };

            // Act
            var response = await client.PostAsJsonAsync($"{Endpoint}/999999/ajuste", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
