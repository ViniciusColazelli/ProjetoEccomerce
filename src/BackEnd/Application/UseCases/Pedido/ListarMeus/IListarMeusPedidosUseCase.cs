using Application.Request;
using Application.Response;

namespace Application.UseCases.Pedido.ListarMeus
{
    public interface IListarMeusPedidosUseCase
    {
        public Task<ResponsePedidos> Execute(RequestListarMeusPedidos request);
    }
}
