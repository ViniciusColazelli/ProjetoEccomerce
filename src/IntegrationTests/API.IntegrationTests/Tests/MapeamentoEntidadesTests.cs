using API.IntegrationTests.Fixture;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.IntegrationTests.Tests
{
    // Garante que as entidades do EF batem com as tabelas criadas pelo FluentMigrator
    public class MapeamentoEntidadesTests : BaseTesteIntegracao
    {
        public MapeamentoEntidadesTests(EcommerceFixture fixture) : base(fixture) { }

        // ════════════════════════════════════════════════════════════════
        // PERSISTÊNCIA
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_PersistirELerCatalogoCompleto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var varianteId = await PrepararProdutoComVariante(ct, quantidade: 5);

            // Act
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            var produto = await dbContext.Produtos
                .Include(p => p.Opcoes)
                .Include(p => p.Variantes).ThenInclude(v => v.TamanhoOpcao)
                .Include(p => p.Variantes).ThenInclude(v => v.CorOpcao)
                .Include(p => p.Variantes).ThenInclude(v => v.Estoque)
                .SingleAsync(ct);

            // Assert
            produto.Opcoes.Should().HaveCount(2);
            produto.Opcoes.Should().OnlyContain(o => o.Ativo);

            var variante = produto.Variantes.Should().ContainSingle().Subject;
            variante.Id.Should().Be(varianteId);
            variante.TamanhoOpcao.Tipo.Should().Be(TipoOpcao.Tamanho);
            variante.TamanhoOpcao.Valor.Should().Be("M");
            variante.CorOpcao!.Tipo.Should().Be(TipoOpcao.Cor);
            variante.Estoque!.Quantidade.Should().Be(5);
        }

        [Fact]
        public async Task Should_GravarTipoComoTexto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await PrepararProdutoComVariante(ct, quantidade: 0);

            // Act
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            var tipos = await dbContext.Database
                .SqlQueryRaw<string>("SELECT tipo AS \"Value\" FROM produto_opcoes ORDER BY tipo")
                .ToListAsync(ct);

            // Assert
            tipos.Should().Equal("Cor", "Tamanho");
        }

        [Fact]
        public async Task Should_PreencherCriadoEm_When_MovimentacaoGravada()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var varianteId = await PrepararProdutoComVariante(ct, quantidade: 10);

            using (var scope = _fixture.CriarScope())
            {
                var dbContext = _fixture.GetDbContext(scope);

                dbContext.EstoqueMovimentacoes.Add(new EstoqueMovimentacoes
                {
                    VarianteId = varianteId,
                    Tipo = TipoMovimentacao.Entrada,
                    Quantidade = 10,
                    Motivo = "Estoque inicial"
                });

                await dbContext.SaveChangesAsync(ct);
            }

            // Act
            using var scopeLeitura = _fixture.CriarScope();
            var movimentacao = await _fixture.GetDbContext(scopeLeitura).EstoqueMovimentacoes.SingleAsync(ct);

            // Assert
            movimentacao.Tipo.Should().Be(TipoMovimentacao.Entrada);
            movimentacao.Motivo.Should().Be("Estoque inicial");
            movimentacao.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        }

        // ════════════════════════════════════════════════════════════════
        // REGRAS DO BANCO
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_RejeitarOpcaoDuplicada_IgnorandoMaiusculas()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await PrepararProdutoComVariante(ct, quantidade: 0);

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            var produtoId = await dbContext.Produtos.Select(p => p.Id).SingleAsync(ct);

            dbContext.ProdutoOpcoes.Add(new ProdutoOpcoes { ProdutoId = produtoId, Tipo = TipoOpcao.Tamanho, Valor = "m" });

            // Act
            var acao = () => dbContext.SaveChangesAsync(ct);

            // Assert
            await acao.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task Should_RejeitarEstoqueNegativo()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await PrepararProdutoComVariante(ct, quantidade: 0);

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            var estoque = await dbContext.Estoque.SingleAsync(ct);

            estoque.Quantidade = -1;

            // Act
            var acao = () => dbContext.SaveChangesAsync(ct);

            // Assert
            await acao.Should().ThrowAsync<DbUpdateException>();
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        // Cria um produto com tamanho M, cor Azul, a variante M/Azul e o estoque dela
        private async Task<int> PrepararProdutoComVariante(CancellationToken ct, int quantidade)
        {
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            var tamanho = new ProdutoOpcoes { Tipo = TipoOpcao.Tamanho, Valor = "M" };
            var cor = new ProdutoOpcoes { Tipo = TipoOpcao.Cor, Valor = "Azul" };

            var variante = new ProdutoVariantes
            {
                TamanhoOpcao = tamanho,
                CorOpcao = cor,
                Estoque = new Estoque { Quantidade = quantidade }
            };

            dbContext.Produtos.Add(new Produtos
            {
                Nome = "Camiseta Polo",
                PrecoBase = 59.90m,
                Ativo = true,
                Opcoes = { tamanho, cor },
                Variantes = { variante }
            });

            await dbContext.SaveChangesAsync(ct);

            return variante.Id;
        }
    }
}
