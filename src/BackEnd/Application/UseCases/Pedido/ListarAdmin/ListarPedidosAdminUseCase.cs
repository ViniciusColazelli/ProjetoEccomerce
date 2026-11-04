using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Pedido.ListarAdmin
{
    public class ListarPedidosAdminUseCase : IListarPedidosAdminUseCase
    {
        private readonly IPedidoRepository _pedidoRepository;

        public ListarPedidosAdminUseCase(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository;
        }

        public async Task<ResponsePedidosAdmin> Execute(RequestListarPedidosAdmin request)
        {
            Validate(request);

            var filtro = MapearRequest.RequestParaFiltro(request);

            var pedidos = await _pedidoRepository.Listar(filtro);
            var total = await _pedidoRepository.Contar(filtro);

            return new ResponsePedidosAdmin
            {
                Pedidos = pedidos.Select(MapearPedido.ParaResumoAdmin).ToList(),
                Pagina = request.Pagina,
                TamanhoPagina = request.TamanhoPagina,
                Total = total
            };
        }

        private static void Validate(RequestListarPedidosAdmin request)
        {
            var validator = new ListarPedidosAdminValidator();

            var result = validator.Validate(request);

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}
