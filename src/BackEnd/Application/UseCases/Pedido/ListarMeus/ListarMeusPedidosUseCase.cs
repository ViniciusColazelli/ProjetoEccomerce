using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Domain.Extensions;
using Domain.Filtros;
using Domain.Repositories;
using Domain.Services.ClienteLogado;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Pedido.ListarMeus
{
    public class ListarMeusPedidosUseCase : IListarMeusPedidosUseCase
    {
        private readonly IClienteLogado _clienteLogado;
        private readonly IPedidoRepository _pedidoRepository;

        public ListarMeusPedidosUseCase(IClienteLogado clienteLogado, IPedidoRepository pedidoRepository)
        {
            _clienteLogado = clienteLogado;
            _pedidoRepository = pedidoRepository;
        }

        public async Task<ResponsePedidos> Execute(RequestListarMeusPedidos request)
        {
            var cliente = await _clienteLogado.Cliente();

            Validate(request);

            // o ClienteId vem do token, nunca da requisição
            var filtro = new FiltroListagemPedidos
            {
                ClienteId = cliente.Id,
                Pagina = request.Pagina,
                TamanhoPagina = request.TamanhoPagina
            };

            var pedidos = await _pedidoRepository.Listar(filtro);
            var total = await _pedidoRepository.Contar(filtro);

            return new ResponsePedidos
            {
                Pedidos = pedidos.Select(MapearPedido.ParaResumo).ToList(),
                Pagina = request.Pagina,
                TamanhoPagina = request.TamanhoPagina,
                Total = total
            };
        }

        private static void Validate(RequestListarMeusPedidos request)
        {
            var validator = new ListarMeusPedidosValidator();

            var result = validator.Validate(request);

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}
