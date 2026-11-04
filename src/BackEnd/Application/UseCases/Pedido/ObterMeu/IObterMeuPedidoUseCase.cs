using Application.Response;

namespace Application.UseCases.Pedido.ObterMeu
{
    public interface IObterMeuPedidoUseCase
    {
        public Task<ResponsePedido> Execute(int pedidoId);
    }
}
