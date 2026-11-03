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
    public class AdminEstoqueMovimentacaoTests : BaseTesteIntegracao
    {
        public AdminEstoqueMovimentacaoTests(EcommerceFixture fixture) : base(fixture) { }

        private const string Endpoint = "/api/admin/estoque/movimentacoes";

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
        public async Task Should_ListarMaisRecentesPrimeiro_ComDadosDaVariante()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M", "Branco");
            await PrepararMovimentacao(ct, variante.Id, TipoMovimentacao.Entrada, 10, new DateTime(2026, 3, 1, 9, 0, 0), "Estoque inicial");
            await PrepararMovimentacao(ct, variante.Id, TipoMovimentacao.Saida, 2, new DateTime(2026, 3, 5, 9, 0, 0), "Perda");

            // Act
            var response = await client.GetAsync(Endpoint, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseMovimentacoes>(EcommerceFixture.JsonOptions, ct);
            body!.Total.Should().Be(2);
            body.Movimentacoes.Select(m => m.Motivo).Should().Equal("Perda", "Estoque inicial");

            var maisRecente = body.Movimentacoes[0];
            maisRecente.VarianteId.Should().Be(variante.Id);
            maisRecente.ProdutoId.Should().Be(variante.ProdutoId);
            maisRecente.Produto.Should().Be("Jaleco");
            maisRecente.Tamanho.Should().Be("M");
            maisRecente.Cor.Should().Be("Branco");
            maisRecente.Tipo.Should().Be(TipoMovimentacao.Saida);
            maisRecente.Quantidade.Should().Be(2);
        }

        [Fact]
        public async Task Should_FiltrarPorVariante()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var jaleco = await PrepararVariante(ct, "Jaleco", "M");
            var avental = await PrepararVariante(ct, "Avental", "G");
            await PrepararMovimentacao(ct, jaleco.Id, TipoMovimentacao.Entrada, 5, new DateTime(2026, 3, 1, 9, 0, 0));
            await PrepararMovimentacao(ct, avental.Id, TipoMovimentacao.Entrada, 7, new DateTime(2026, 3, 1, 9, 0, 0));

            // Act
            var body = await client.GetFromJsonAsync<ResponseMovimentacoes>($"{Endpoint}?varianteId={jaleco.Id}", EcommerceFixture.JsonOptions, ct);

            // Assert
            body!.Total.Should().Be(1);
            body.Movimentacoes.Should().ContainSingle(m => m.VarianteId == jaleco.Id);
        }

        [Fact]
        public async Task Should_FiltrarPorProduto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var jaleco = await PrepararVariante(ct, "Jaleco", "M");
            var avental = await PrepararVariante(ct, "Avental", "G");
            await PrepararMovimentacao(ct, jaleco.Id, TipoMovimentacao.Entrada, 5, new DateTime(2026, 3, 1, 9, 0, 0));
            await PrepararMovimentacao(ct, avental.Id, TipoMovimentacao.Entrada, 7, new DateTime(2026, 3, 1, 9, 0, 0));

            // Act
            var body = await client.GetFromJsonAsync<ResponseMovimentacoes>($"{Endpoint}?produtoId={avental.ProdutoId}", EcommerceFixture.JsonOptions, ct);

            // Assert
            body!.Total.Should().Be(1);
            body.Movimentacoes.Should().ContainSingle(m => m.ProdutoId == avental.ProdutoId && m.Quantidade == 7);
        }

        [Theory]
        [InlineData("Entrada", TipoMovimentacao.Entrada)]
        [InlineData("saida", TipoMovimentacao.Saida)]
        public async Task Should_FiltrarPorTipo(string tipo, TipoMovimentacao tipoEsperado)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M");
            await PrepararMovimentacao(ct, variante.Id, TipoMovimentacao.Entrada, 5, new DateTime(2026, 3, 1, 9, 0, 0));
            await PrepararMovimentacao(ct, variante.Id, TipoMovimentacao.Saida, 1, new DateTime(2026, 3, 2, 9, 0, 0));

            // Act
            var body = await client.GetFromJsonAsync<ResponseMovimentacoes>($"{Endpoint}?tipo={tipo}", EcommerceFixture.JsonOptions, ct);

            // Assert
            body!.Total.Should().Be(1);
            body.Movimentacoes.Should().ContainSingle(m => m.Tipo == tipoEsperado);
        }

        [Fact]
        public async Task Should_FiltrarPorPeriodo_IncluindoODiaFinalInteiro()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M");
            await PrepararMovimentacao(ct, variante.Id, TipoMovimentacao.Entrada, 1, new DateTime(2026, 2, 28, 23, 59, 0), "Antes");
            await PrepararMovimentacao(ct, variante.Id, TipoMovimentacao.Entrada, 1, new DateTime(2026, 3, 1, 0, 0, 0), "Primeiro dia");
            await PrepararMovimentacao(ct, variante.Id, TipoMovimentacao.Entrada, 1, new DateTime(2026, 3, 10, 23, 30, 0), "Último dia");
            await PrepararMovimentacao(ct, variante.Id, TipoMovimentacao.Entrada, 1, new DateTime(2026, 3, 11, 0, 0, 0), "Depois");

            // Act
            var body = await client.GetFromJsonAsync<ResponseMovimentacoes>($"{Endpoint}?de=2026-03-01&ate=2026-03-10", EcommerceFixture.JsonOptions, ct);

            // Assert
            body!.Movimentacoes.Select(m => m.Motivo).Should().Equal("Último dia", "Primeiro dia");
        }

        [Fact]
        public async Task Should_PaginarETotalizar()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, "Jaleco", "M");
            for (var dia = 1; dia <= 3; dia++)
                await PrepararMovimentacao(ct, variante.Id, TipoMovimentacao.Entrada, dia, new DateTime(2026, 3, dia, 9, 0, 0));

            // Act
            var body = await client.GetFromJsonAsync<ResponseMovimentacoes>($"{Endpoint}?pagina=2&tamanhoPagina=2", EcommerceFixture.JsonOptions, ct);

            // Assert
            body!.Total.Should().Be(3);
            body.Movimentacoes.Should().ContainSingle(m => m.Quantidade == 1);
        }

        [Theory]
        [InlineData("tipo=Ajuste", "Tipo inválido. Use Entrada ou Saida.")]
        [InlineData("de=2026-03-10&ate=2026-03-01", "A data inicial deve ser menor ou igual à data final.")]
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

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be(mensagemEsperada);
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        private async Task<ProdutoVariantes> PrepararVariante(CancellationToken ct, string nomeProduto, string tamanho, string? cor = null)
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
                Estoque = new Estoque { Quantidade = 0 }
            };
            produto.Variantes.Add(variante);

            await dbContext.Produtos.AddAsync(produto, ct);
            await dbContext.SaveChangesAsync(ct);

            return variante;
        }

        private async Task PrepararMovimentacao(CancellationToken ct, int varianteId, TipoMovimentacao tipo, int quantidade, DateTime criadoEm, string motivo = "Teste")
        {
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            await dbContext.EstoqueMovimentacoes.AddAsync(new EstoqueMovimentacoes
            {
                VarianteId = varianteId,
                Tipo = tipo,
                Quantidade = quantidade,
                Motivo = motivo,
                CriadoEm = DateTime.SpecifyKind(criadoEm, DateTimeKind.Utc)
            }, ct);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
