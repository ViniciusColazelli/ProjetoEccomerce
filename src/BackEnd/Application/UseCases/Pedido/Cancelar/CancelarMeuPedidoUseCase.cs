using Application.Response;
using Application.Services.Mapeamento;
using Domain.Enums;
using Domain.Repositories;
using Domain.Services.ClienteLogado;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Pedido.Cancelar
{
    // O cliente só cancela antes do pagamento: nessa etapa o estoque ainda não foi baixado,
    // então não há nada para devolver. Depois de pago, o pedido não pode mais ser cancelado (nem pelo admin).
    public class CancelarMeuPedidoUseCase : ICancelarMeuPedidoUseCase
    {
        private readonly IClienteLogado _clienteLogado;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public CancelarMeuPedidoUseCase(IClienteLogado clienteLogado, IPedidoRepository pedidoRepository, ISalvarDBRepository salvarDB)
        {
            _clienteLogado = clienteLogado;
            _pedidoRepository = pedidoRepository;
            _salvarDB = salvarDB;
        }

        public async Task<ResponsePedido> Execute(int pedidoId)
        {
            var cliente = await _clienteLogado.Cliente();

            var pedido = await _pedidoRepository.GetById(pedidoId);

            if (pedido is null || pedido.ClienteId != cliente.Id)
                throw new NaoEncontradoException(ResourceMensagensDeErro.PEDIDO_NAO_ENCONTRADO);

            if (pedido.Status != StatusPedido.AguardandoPagamento)
                throw new ConflitoException(ResourceMensagensDeErro.PEDIDO_NAO_PODE_SER_CANCELADO);

            // o pedido veio rastreado: o UPDATE leva só status e atualizado_em.
            // (Update() marcaria todos os campos, e regravar o criado_em lido do banco falha no Npgsql)
            pedido.Status = StatusPedido.Cancelado;
            pedido.AtualizadoEm = DateTime.UtcNow;

            // se o admin mudou o status no meio do caminho (ex.: confirmou o pagamento), o xmin barra com 409
            await _salvarDB.Salvar();

            var pedidoSalvo = (await _pedidoRepository.ObterComDetalhes(pedido.Id))!;

            return MapearPedido.ParaResponse(pedidoSalvo);
        }
    }
}
