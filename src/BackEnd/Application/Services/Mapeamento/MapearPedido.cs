using Application.Response;
using Domain.Entities;

namespace Application.Services.Mapeamento
{
    public static class MapearPedido
    {
        // Espera o pedido carregado com os itens
        public static ResponsePedidoResumo ParaResumo(Pedidos pedido) => PreencherResumo<ResponsePedidoResumo>(pedido);

        // Espera o pedido carregado com o cliente e os itens
        public static ResponsePedidoResumoAdmin ParaResumoAdmin(Pedidos pedido)
        {
            var resumo = PreencherResumo<ResponsePedidoResumoAdmin>(pedido);
            resumo.AtualizadoEm = pedido.AtualizadoEm;
            resumo.Cliente = ParaCliente(pedido.Cliente);

            return resumo;
        }

        // Espera o pedido carregado com itens, produto, variante (tamanho e cor) e customizações
        public static ResponsePedido ParaResponse(Pedidos pedido) => Preencher<ResponsePedido>(pedido);

        // O mesmo, com o cliente
        public static ResponsePedidoAdmin ParaResponseAdmin(Pedidos pedido)
        {
            var response = Preencher<ResponsePedidoAdmin>(pedido);
            response.Cliente = ParaCliente(pedido.Cliente);

            return response;
        }

        private static T PreencherResumo<T>(Pedidos pedido) where T : ResponsePedidoResumo, new()
        {
            return new T
            {
                Id = pedido.Id,
                Status = pedido.Status,
                Total = pedido.Total,
                QuantidadePecas = pedido.Itens.Sum(item => item.Quantidade),
                CriadoEm = pedido.CriadoEm
            };
        }

        private static T Preencher<T>(Pedidos pedido) where T : ResponsePedido, new()
        {
            return new T
            {
                Id = pedido.Id,
                Status = pedido.Status,
                Total = pedido.Total,
                Observacao = pedido.Observacao,
                CriadoEm = pedido.CriadoEm,
                AtualizadoEm = pedido.AtualizadoEm,
                Itens = pedido.Itens.OrderBy(item => item.Id).Select(item => new ResponsePedidoItem
                {
                    Id = item.Id,
                    ProdutoId = item.ProdutoId,
                    Produto = item.Produto.Nome,
                    VarianteId = item.VarianteId,
                    Tamanho = item.Variante?.TamanhoOpcao.Valor,
                    Cor = item.Variante?.CorOpcao?.Valor,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    Subtotal = item.PrecoUnitario * item.Quantidade,
                    Personalizacoes = item.Customizacoes.OrderBy(customizacao => customizacao.Id).Select(customizacao => new ResponsePedidoPersonalizacao
                    {
                        OpcaoId = customizacao.OpcaoId,
                        Valor = customizacao.Valor,
                        PrecoAdicional = customizacao.PrecoAdicional
                    }).ToList()
                }).ToList()
            };
        }

        private static ResponsePedidoCliente ParaCliente(Clientes cliente) => new()
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            Email = cliente.Email
        };
    }
}
