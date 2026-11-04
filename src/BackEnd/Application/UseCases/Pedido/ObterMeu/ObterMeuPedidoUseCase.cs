using Application.Response;
using Application.Services.Mapeamento;
using Domain.Repositories;
using Domain.Services.ClienteLogado;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Pedido.ObterMeu
{
    public class ObterMeuPedidoUseCase : IObterMeuPedidoUseCase
    {
        private readonly IClienteLogado _clienteLogado;
        private readonly IPedidoRepository _pedidoRepository;

        public ObterMeuPedidoUseCase(IClienteLogado clienteLogado, IPedidoRepository pedidoRepository)
        {
            _clienteLogado = clienteLogado;
            _pedidoRepository = pedidoRepository;
        }

        public async Task<ResponsePedido> Execute(int pedidoId)
        {
            var cliente = await _clienteLogado.Cliente();

            var pedido = await _pedidoRepository.ObterComDetalhes(pedidoId);

            // pedido de outro cliente responde igual a inexistente: não revela que o id existe
            if (pedido is null || pedido.ClienteId != cliente.Id)
                throw new NaoEncontradoException(ResourceMensagensDeErro.PEDIDO_NAO_ENCONTRADO);

            return MapearPedido.ParaResponse(pedido);
        }
    }
}
