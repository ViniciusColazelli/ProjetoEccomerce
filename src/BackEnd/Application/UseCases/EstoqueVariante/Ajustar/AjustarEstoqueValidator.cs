using Application.Request;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.EstoqueVariante.Ajustar
{
    public class AjustarEstoqueValidator : AbstractValidator<RequestAjustarEstoque>
    {
        public const int TAMANHO_MAXIMO_MOTIVO = 200;

        public AjustarEstoqueValidator()
        {
            RuleFor(request => request.NovaQuantidade).GreaterThanOrEqualTo(0).WithMessage(ResourceMensagensDeErro.NOVA_QUANTIDADE_INVALIDA);
            RuleFor(request => request.Motivo).NotEmpty().WithMessage(ResourceMensagensDeErro.MOTIVO_OBRIGATORIO);
            RuleFor(request => request.Motivo).MaximumLength(TAMANHO_MAXIMO_MOTIVO).WithMessage(ResourceMensagensDeErro.MOTIVO_MUITO_LONGO);
        }
    }
}
