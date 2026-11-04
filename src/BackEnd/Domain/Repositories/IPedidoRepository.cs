using Domain.Entities;
using Domain.Filtros;

namespace Domain.Repositories
{
    public interface IPedidoRepository
    {
        public Task Adicionar(Pedidos pedido);

        // pedido com cliente, itens, produto, variante (tamanho e cor) e customizações, sem rastreamento
        public Task<Pedidos?> ObterComDetalhes(int pedidoId);

        // rastreado e com os itens, para alterar o status: basta mudar as propriedades e salvar
        public Task<Pedidos?> GetById(int pedidoId);

        // mais recentes primeiro, com o cliente e os itens (sem customizações) para o resumo
        public Task<IList<Pedidos>> Listar(FiltroListagemPedidos filtro);

        public Task<int> Contar(FiltroListagemPedidos filtro);
    }
}
