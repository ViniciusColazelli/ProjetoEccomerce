using Application.Request;
using Domain.Enums;
using Domain.Extensions;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.Pedido.ListarAdmin
{
    public class ListarPedidosAdminValidator : AbstractValidator<RequestListarPedidosAdmin>
    {
        public const int TAMANHO_MAXIMO_PAGINA = 50;
        public const int TAMANHO_MAXIMO_BUSCA = 200;

        public ListarPedidosAdminValidator()
        {
            RuleFor(request => request.Pagina).GreaterThanOrEqualTo(1).WithMessage(ResourceMensagensDeErro.PAGINA_INVALIDA);
            RuleFor(request => request.TamanhoPagina).InclusiveBetween(1, TAMANHO_MAXIMO_PAGINA).WithMessage(ResourceMensagensDeErro.TAMANHO_PAGINA_INVALIDO);
            When(request => string.IsNullOrWhiteSpace(request.Status).IsFalse(), () =>
            {
                RuleFor(request => request.Status)
                    .Must(status => Enum.GetNames<StatusPedido>().Contains(status!.Trim(), StringComparer.OrdinalIgnoreCase))
                    .WithMessage(ResourceMensagensDeErro.STATUS_PEDIDO_INVALIDO);
            });
            RuleFor(request => request.Cliente).MaximumLength(TAMANHO_MAXIMO_BUSCA).WithMessage(ResourceMensagensDeErro.BUSCA_CLIENTE_MUITO_LONGA);
            When(request => request.De.HasValue && request.Ate.HasValue, () =>
            {
                RuleFor(request => request.De)
                    .LessThanOrEqualTo(request => request.Ate)
                    .WithMessage(ResourceMensagensDeErro.PERIODO_INVALIDO);
            });
        }
    }
}
