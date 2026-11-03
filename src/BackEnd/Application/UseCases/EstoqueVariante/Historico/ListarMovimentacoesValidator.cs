using Application.Request;
using Domain.Enums;
using Domain.Extensions;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.EstoqueVariante.Historico
{
    public class ListarMovimentacoesValidator : AbstractValidator<RequestListarMovimentacoes>
    {
        public const int TAMANHO_MAXIMO_PAGINA = 50;

        public ListarMovimentacoesValidator()
        {
            RuleFor(request => request.Pagina).GreaterThanOrEqualTo(1).WithMessage(ResourceMensagensDeErro.PAGINA_INVALIDA);
            RuleFor(request => request.TamanhoPagina).InclusiveBetween(1, TAMANHO_MAXIMO_PAGINA).WithMessage(ResourceMensagensDeErro.TAMANHO_PAGINA_INVALIDO);
            When(request => string.IsNullOrWhiteSpace(request.Tipo).IsFalse(), () =>
            {
                RuleFor(request => request.Tipo)
                    .Must(tipo => Enum.GetNames<TipoMovimentacao>().Contains(tipo!.Trim(), StringComparer.OrdinalIgnoreCase))
                    .WithMessage(ResourceMensagensDeErro.TIPO_MOVIMENTACAO_INVALIDO);
            });
            When(request => request.De.HasValue && request.Ate.HasValue, () =>
            {
                RuleFor(request => request.De)
                    .LessThanOrEqualTo(request => request.Ate)
                    .WithMessage(ResourceMensagensDeErro.PERIODO_INVALIDO);
            });
        }
    }
}
