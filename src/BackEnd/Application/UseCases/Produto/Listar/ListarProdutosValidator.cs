using Application.Request;
using Domain.Extensions;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.Produto.Listar
{
    public class ListarProdutosValidator : AbstractValidator<RequestListarProdutos>
    {
        public const int TAMANHO_MAXIMO_PAGINA = 50;
        public const int TAMANHO_MAXIMO_NOME = 200;

        private static readonly string[] ORDENACOES = { "nome", "preco" };
        private static readonly string[] DIRECOES = { "asc", "desc" };

        public ListarProdutosValidator()
        {
            RuleFor(request => request.Pagina).GreaterThanOrEqualTo(1).WithMessage(ResourceMensagensDeErro.PAGINA_INVALIDA);
            RuleFor(request => request.TamanhoPagina).InclusiveBetween(1, TAMANHO_MAXIMO_PAGINA).WithMessage(ResourceMensagensDeErro.TAMANHO_PAGINA_INVALIDO);
            RuleFor(request => request.Nome).MaximumLength(TAMANHO_MAXIMO_NOME).WithMessage(ResourceMensagensDeErro.BUSCA_NOME_MUITO_LONGA);
            When(request => string.IsNullOrWhiteSpace(request.OrdenarPor).IsFalse(), () =>
            {
                RuleFor(request => request.OrdenarPor)
                    .Must(ordenarPor => ORDENACOES.Contains(ordenarPor!.Trim(), StringComparer.OrdinalIgnoreCase))
                    .WithMessage(ResourceMensagensDeErro.ORDENACAO_INVALIDA);
            });
            When(request => string.IsNullOrWhiteSpace(request.Direcao).IsFalse(), () =>
            {
                RuleFor(request => request.Direcao)
                    .Must(direcao => DIRECOES.Contains(direcao!.Trim(), StringComparer.OrdinalIgnoreCase))
                    .WithMessage(ResourceMensagensDeErro.DIRECAO_INVALIDA);
            });
        }
    }
}
