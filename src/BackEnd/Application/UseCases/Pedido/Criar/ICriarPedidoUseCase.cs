using Application.Request;
using Application.Response;

namespace Application.UseCases.Pedido.Criar
{
    public interface ICriarPedidoUseCase
    {
        public Task<ResponsePedido> Execute(RequestCriarPedido request);
    }
}
