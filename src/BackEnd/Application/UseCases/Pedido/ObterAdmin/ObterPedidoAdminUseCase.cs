using Application.Response;
using Application.Services.Mapeamento;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Pedido.ObterAdmin
{
    public class ObterPedidoAdminUseCase : IObterPedidoAdminUseCase
    {
        private readonly IPedidoRepository _pedidoRepository;

        public ObterPedidoAdminUseCase(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository;
        }

        public async Task<ResponsePedidoAdmin> Execute(int pedidoId)
        {
            var pedido = await _pedidoRepository.ObterComDetalhes(pedidoId);

            if (pedido is null)
                throw new NaoEncontradoException(ResourceMensagensDeErro.PEDIDO_NAO_ENCONTRADO);

            return MapearPedido.ParaResponseAdmin(pedido);
        }
    }
}
