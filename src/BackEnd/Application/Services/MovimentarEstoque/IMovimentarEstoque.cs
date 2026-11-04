using Domain.Entities;

namespace Application.Services.MovimentarEstoque
{
    public interface IMovimentarEstoque
    {
        // pedidoId: o pedido que causou a movimentação (baixa no pagamento, devolução no cancelamento)
        public Task<Estoque> Entrada(int varianteId, int quantidade, string motivo, int? pedidoId = null);

        public Task<Estoque> Saida(int varianteId, int quantidade, string motivo, int? pedidoId = null);
    }
}
