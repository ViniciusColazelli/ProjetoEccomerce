using Application.Response;

namespace Application.UseCases.Pedido.Cancelar
{
    public interface ICancelarMeuPedidoUseCase
    {
        public Task<ResponsePedido> Execute(int pedidoId);
    }
}
