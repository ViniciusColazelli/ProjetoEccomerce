using Application.Request;
using Application.Response;

namespace Application.UseCases.Pedido.ListarAdmin
{
    public interface IListarPedidosAdminUseCase
    {
        public Task<ResponsePedidosAdmin> Execute(RequestListarPedidosAdmin request);
    }
}
