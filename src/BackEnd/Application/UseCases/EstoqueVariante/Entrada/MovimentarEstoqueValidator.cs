using Application.Request;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.EstoqueVariante.Entrada
{
    public class MovimentarEstoqueValidator : AbstractValidator<RequestMovimentarEstoque>
    {
        public const int TAMANHO_MAXIMO_MOTIVO = 200;

        public MovimentarEstoqueValidator()
        {
            RuleFor(request => request.Quantidade).GreaterThan(0).WithMessage(ResourceMensagensDeErro.QUANTIDADE_INVALIDA);
            RuleFor(request => request.Motivo).NotEmpty().WithMessage(ResourceMensagensDeErro.MOTIVO_OBRIGATORIO);
            RuleFor(request => request.Motivo).MaximumLength(TAMANHO_MAXIMO_MOTIVO).WithMessage(ResourceMensagensDeErro.MOTIVO_MUITO_LONGO);
        }
    }
}
