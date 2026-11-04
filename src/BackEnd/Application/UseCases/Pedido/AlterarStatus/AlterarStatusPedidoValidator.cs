using Application.Request;
using Domain.Enums;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.Pedido.AlterarStatus
{
    public class AlterarStatusPedidoValidator : AbstractValidator<RequestAlterarStatusPedido>
    {
        public AlterarStatusPedidoValidator()
        {
            RuleFor(request => request.Status)
                .Must(status => string.IsNullOrWhiteSpace(status) == false &&
                    Enum.GetNames<StatusPedido>().Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage(ResourceMensagensDeErro.STATUS_PEDIDO_INVALIDO);
        }
    }
}
