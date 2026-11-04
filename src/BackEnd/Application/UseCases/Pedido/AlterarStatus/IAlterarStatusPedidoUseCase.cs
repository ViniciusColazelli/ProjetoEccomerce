using Application.Request;
using Application.Response;

namespace Application.UseCases.Pedido.AlterarStatus
{
    public interface IAlterarStatusPedidoUseCase
    {
        public Task<ResponsePedidoAdmin> Execute(int pedidoId, RequestAlterarStatusPedido request);
    }
}
