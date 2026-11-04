using Application.Response;

namespace Application.UseCases.Pedido.ObterAdmin
{
    public interface IObterPedidoAdminUseCase
    {
        public Task<ResponsePedidoAdmin> Execute(int pedidoId);
    }
}
