using Domain.Entities;
using Domain.Enums;

namespace API.IntegrationTests.Fixture
{
    // Monta catálogo e pedidos direto no banco, para os testes de consulta e de status não dependerem do POST /api/pedidos
    public static class CenarioPedidos
    {
        public const decimal PRECO_BASE = 59.90m;
        public const decimal PRECO_BORDADO = 15m;

        // VarianteId é M/Azul; VarianteGId é G/Azul
        public record Catalogo(int ProdutoId, int VarianteId, ProdutoOpcoes Bordado, int VarianteGId);

        public static DateTime Dia(int dia) => new(2026, 10, dia, 12, 0, 0, DateTimeKind.Utc);

        // Camiseta Polo (59,90), M/Azul e G/Azul com os estoques informados, e Bordado (+15)
        public static async Task<Catalogo> PrepararCatalogo(EcommerceFixture fixture, CancellationToken ct, int estoque = 10, int estoqueG = 10)
        {
            using var scope = fixture.CriarScope();
            var dbContext = fixture.GetDbContext(scope);

            var m = new ProdutoOpcoes { Tipo = TipoOpcao.Tamanho, Valor = "M" };
            var g = new ProdutoOpcoes { Tipo = TipoOpcao.Tamanho, Valor = "G" };
            var azul = new ProdutoOpcoes { Tipo = TipoOpcao.Cor, Valor = "Azul" };
            var bordado = new ProdutoOpcoes { Tipo = TipoOpcao.Personalizacao, Valor = "Bordado", PrecoAdicional = PRECO_BORDADO };
            var mAzul = new ProdutoVariantes { TamanhoOpcao = m, CorOpcao = azul, Ativo = true, Estoque = new Estoque { Quantidade = estoque } };
            var gAzul = new ProdutoVariantes { TamanhoOpcao = g, CorOpcao = azul, Ativo = true, Estoque = new Estoque { Quantidade = estoqueG } };

            var produto = new Produtos
            {
                Nome = "Camiseta Polo",
                PrecoBase = PRECO_BASE,
                Ativo = true,
                Opcoes = { m, g, azul, bordado },
                Variantes = { mAzul, gAzul }
            };

            dbContext.Produtos.Add(produto);
            await dbContext.SaveChangesAsync(ct);

            return new Catalogo(produto.Id, mAzul.Id, bordado, gAzul.Id);
        }

        // Pedido de um item só, da variante M/Azul
        public static Task<int> PrepararPedido(EcommerceFixture fixture, CancellationToken ct, Catalogo catalogo, int clienteId, StatusPedido status, DateTime criadoEm,
            int quantidade = 1, bool comBordado = false, DateTime? atualizadoEm = null)
        {
            return PrepararPedidoComItens(fixture, ct, catalogo, clienteId, status, criadoEm, atualizadoEm, (catalogo.VarianteId, quantidade, comBordado));
        }

        // varianteId nulo: item antigo, anterior às variantes
        public static async Task<int> PrepararPedidoComItens(EcommerceFixture fixture, CancellationToken ct, Catalogo catalogo, int clienteId, StatusPedido status, DateTime criadoEm,
            DateTime? atualizadoEm, params (int? VarianteId, int Quantidade, bool ComBordado)[] itens)
        {
            using var scope = fixture.CriarScope();
            var dbContext = fixture.GetDbContext(scope);

            var pedido = new Pedidos
            {
                ClienteId = clienteId,
                Status = status,
                CriadoEm = criadoEm,
                AtualizadoEm = atualizadoEm
            };

            foreach (var (varianteId, quantidade, comBordado) in itens)
            {
                var item = new PedidoItens
                {
                    ProdutoId = catalogo.ProdutoId,
                    VarianteId = varianteId,
                    Quantidade = quantidade,
                    PrecoUnitario = PRECO_BASE + (comBordado ? PRECO_BORDADO : 0m)
                };

                if (comBordado)
                    item.Customizacoes.Add(new PedidoItemCustomizacoes { OpcaoId = catalogo.Bordado.Id, Tipo = TipoOpcao.Personalizacao, Valor = "Bordado", PrecoAdicional = PRECO_BORDADO });

                pedido.Itens.Add(item);
            }

            pedido.Total = pedido.Itens.Sum(item => item.PrecoUnitario * item.Quantidade);

            dbContext.Pedidos.Add(pedido);
            await dbContext.SaveChangesAsync(ct);

            return pedido.Id;
        }
    }
}
