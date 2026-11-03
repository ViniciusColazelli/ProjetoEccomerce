using Application.Request;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.EstoqueVariante.Listar
{
    public class ListarEstoqueValidator : AbstractValidator<RequestListarEstoque>
    {
        public const int TAMANHO_MAXIMO_PAGINA = 50;

        public ListarEstoqueValidator()
        {
            RuleFor(request => request.Pagina).GreaterThanOrEqualTo(1).WithMessage(ResourceMensagensDeErro.PAGINA_INVALIDA);
            RuleFor(request => request.TamanhoPagina).InclusiveBetween(1, TAMANHO_MAXIMO_PAGINA).WithMessage(ResourceMensagensDeErro.TAMANHO_PAGINA_INVALIDO);
        }
    }
}
