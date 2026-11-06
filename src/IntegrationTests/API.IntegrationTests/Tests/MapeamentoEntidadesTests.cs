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

        [Fact]
        public async Task Should_PersistirELerPedidoCompleto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var varianteId = await PrepararProdutoComVariante(ct, quantidade: 5);
            var pedidoId = await PrepararPedido(ct, varianteId);

            // Act
            using var scope = _fixture.CriarScope();
            var pedido = await _fixture.GetDbContext(scope).Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Itens).ThenInclude(i => i.Produto)
                .Include(p => p.Itens).ThenInclude(i => i.Variante)
                .Include(p => p.Itens).ThenInclude(i => i.Customizacoes).ThenInclude(c => c.Opcao)
                .SingleAsync(p => p.Id == pedidoId, ct);

            // Assert
            pedido.Status.Should().Be(StatusPedido.AguardandoPagamento);
            pedido.Total.Should().Be(149.80m);
            pedido.Observacao.Should().Be("Entregar na recepção");
            pedido.Cliente.Email.Should().Be("pedido@email.com");
            pedido.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
            pedido.AtualizadoEm.Should().BeNull();

            var item = pedido.Itens.Should().ContainSingle().Subject;
            item.Produto.Nome.Should().Be("Camiseta Polo");
            item.Variante!.Id.Should().Be(varianteId);
            item.Quantidade.Should().Be(2);
            item.PrecoUnitario.Should().Be(74.90m);

            var customizacao = item.Customizacoes.Should().ContainSingle().Subject;
            customizacao.Tipo.Should().Be(TipoOpcao.Personalizacao);
            customizacao.Valor.Should().Be("Bordado");
            customizacao.PrecoAdicional.Should().Be(15m);
            customizacao.Opcao.Valor.Should().Be("Bordado");
        }

        // O conversor de datas só muda o Kind: o horário gravado no banco é exatamente o UTC informado
        [Fact]
        public async Task Should_GravarDataEmUtcSemDeslocarEDevolverComKindUtc()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var varianteId = await PrepararProdutoComVariante(ct, quantidade: 5);
            var pedidoId = await PrepararPedido(ct, varianteId);

            using (var scope = _fixture.CriarScope())
            {
                var dbContext = _fixture.GetDbContext(scope);
                var pedido = await dbContext.Pedidos.SingleAsync(p => p.Id == pedidoId, ct);
                pedido.AtualizadoEm = new DateTime(2026, 11, 4, 13, 20, 0, DateTimeKind.Utc);
                await dbContext.SaveChangesAsync(ct);
            }

            // Act
            using var scopeLeitura = _fixture.CriarScope();
            var dbLeitura = _fixture.GetDbContext(scopeLeitura);
            var noBanco = await dbLeitura.Database
                .SqlQueryRaw<string>("SELECT to_char(atualizado_em, 'YYYY-MM-DD HH24:MI:SS') AS \"Value\" FROM pedidos")
                .SingleAsync(ct);
            var lido = await dbLeitura.Pedidos.Select(p => p.AtualizadoEm).SingleAsync(ct);

            // Assert
            noBanco.Should().Be("2026-11-04 13:20:00");
            lido!.Value.Kind.Should().Be(DateTimeKind.Utc);
            lido.Value.Should().Be(new DateTime(2026, 11, 4, 13, 20, 0, DateTimeKind.Utc));
        }

        [Fact]
        public async Task Should_GravarStatusDoPedidoComoTexto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var varianteId = await PrepararProdutoComVariante(ct, quantidade: 5);
            await PrepararPedido(ct, varianteId);

            // Act
            using var scope = _fixture.CriarScope();
            var status = await _fixture.GetDbContext(scope).Database
                .SqlQueryRaw<string>("SELECT status AS \"Value\" FROM pedidos")
                .SingleAsync(ct);

            // Assert
            status.Should().Be("AguardandoPagamento");
        }

        // Duas gravações do mesmo pedido a partir da mesma leitura (ex.: dois admins confirmando o pagamento):
        // a segunda precisa falhar, senão o estoque seria baixado duas vezes
        [Fact]
        public async Task Should_LancarConflito_When_PedidoAlteradoPorOutraOperacao()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var varianteId = await PrepararProdutoComVariante(ct, quantidade: 5);
            var pedidoId = await PrepararPedido(ct, varianteId);

            using var scopePrimeiro = _fixture.CriarScope();
            using var scopeSegundo = _fixture.CriarScope();
            var primeiro = await _fixture.GetDbContext(scopePrimeiro).Pedidos.SingleAsync(p => p.Id == pedidoId, ct);
            var segundo = await _fixture.GetDbContext(scopeSegundo).Pedidos.SingleAsync(p => p.Id == pedidoId, ct);

            primeiro.Status = StatusPedido.Pago;
            await _fixture.GetDbContext(scopePrimeiro).SaveChangesAsync(ct);

            segundo.Status = StatusPedido.Pago;

            // Act
            var acao = () => _fixture.GetDbContext(scopeSegundo).SaveChangesAsync(ct);

            // Assert
            await acao.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        // ════════════════════════════════════════════════════════════════
        // REGRAS DO BANCO
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("status")]   // 'Pendente' não é um StatusPedido
        [InlineData("total")]    // -1
        public async Task Should_RejeitarPedidoInvalido(string coluna)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var varianteId = await PrepararProdutoComVariante(ct, quantidade: 5);
            var pedidoId = await PrepararPedido(ct, varianteId);

            using var scope = _fixture.CriarScope();
            var banco = _fixture.GetDbContext(scope).Database;

            // Act: o SQL direto pula a conversão do enum, para o CHECK do banco ser o único a barrar.
            // ExecuteSqlAsync com interpolação vira parâmetros ($1, $2), sem montar SQL com texto
            Func<Task<int>> acao = coluna switch
            {
                "status" => () => banco.ExecuteSqlAsync($"UPDATE pedidos SET status = {"Pendente"} WHERE id = {pedidoId}", ct),
                "total" => () => banco.ExecuteSqlAsync($"UPDATE pedidos SET total = {-1m} WHERE id = {pedidoId}", ct),
                _ => throw new ArgumentOutOfRangeException(nameof(coluna))
            };

            // Assert
            await acao.Should().ThrowAsync<Npgsql.PostgresException>().Where(e => e.SqlState == "23514");
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(1, -1)]
        public async Task Should_RejeitarItemInvalido(int quantidade, decimal precoUnitario)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var varianteId = await PrepararProdutoComVariante(ct, quantidade: 5);
            var pedidoId = await PrepararPedido(ct, varianteId);

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            var produtoId = await dbContext.Produtos.Select(p => p.Id).SingleAsync(ct);

            dbContext.PedidoItens.Add(new PedidoItens
            {
                PedidoId = pedidoId,
                ProdutoId = produtoId,
                VarianteId = varianteId,
                Quantidade = quantidade,
                PrecoUnitario = precoUnitario
            });

            // Act
            var acao = () => dbContext.SaveChangesAsync(ct);

            // Assert
            await acao.Should().ThrowAsync<DbUpdateException>();
        }

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

        [Theory]
        [InlineData(TipoOpcao.Tamanho)]
        [InlineData(TipoOpcao.Cor)]
        public async Task Should_RejeitarPrecoAdicional_When_TamanhoOuCor(TipoOpcao tipo)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await PrepararProdutoComVariante(ct, quantidade: 0);

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            var produtoId = await dbContext.Produtos.Select(p => p.Id).SingleAsync(ct);

            // grava direto pelo EF, sem passar pela validação da API
            dbContext.ProdutoOpcoes.Add(new ProdutoOpcoes { ProdutoId = produtoId, Tipo = tipo, Valor = "Extra", PrecoAdicional = 5m });

            // Act
            var acao = () => dbContext.SaveChangesAsync(ct);

            // Assert: 23514 = violação de CHECK
            await acao.Should().ThrowAsync<DbUpdateException>()
                .Where(e => ((Npgsql.PostgresException)e.InnerException!).SqlState == "23514");
        }

        [Fact]
        public async Task Should_RejeitarEmailDuplicado_IgnorandoMaiusculas()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            await _fixture.CadastrarCliente("ana@email.com", "Senha@123");

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            // grava direto pelo EF, sem passar pela validação da API
            dbContext.Clientes.Add(new Clientes { Nome = "Ana", Email = "Ana@Email.com", Senha = "x" });

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

        // Pedido de 2 unidades da variante, com Bordado (+15) no item: 2 × (59,90 + 15) = 149,80
        private async Task<int> PrepararPedido(CancellationToken ct, int varianteId)
        {
            var cliente = await _fixture.CadastrarCliente("pedido@email.com", "Senha@123");

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            var produtoId = await dbContext.ProdutoVariantes.Where(v => v.Id == varianteId).Select(v => v.ProdutoId).SingleAsync(ct);
            var bordado = new ProdutoOpcoes { ProdutoId = produtoId, Tipo = TipoOpcao.Personalizacao, Valor = "Bordado", PrecoAdicional = 15m };
            dbContext.ProdutoOpcoes.Add(bordado);

            var pedido = new Pedidos
            {
                ClienteId = cliente.Id,
                Total = 149.80m,
                Observacao = "Entregar na recepção",
                Itens =
                {
                    new PedidoItens
                    {
                        ProdutoId = produtoId,
                        VarianteId = varianteId,
                        Quantidade = 2,
                        PrecoUnitario = 74.90m,
                        Customizacoes = { new PedidoItemCustomizacoes { Opcao = bordado, Tipo = TipoOpcao.Personalizacao, Valor = "Bordado", PrecoAdicional = 15m } }
                    }
                }
            };

            dbContext.Pedidos.Add(pedido);
            await dbContext.SaveChangesAsync(ct);

            return pedido.Id;
        }
    }
}
