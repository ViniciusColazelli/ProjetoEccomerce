using Application.Request;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.Pedido.Criar
{
    public class CriarPedidoValidator : AbstractValidator<RequestCriarPedido>
    {
        public const int MAXIMO_ITENS = 50;
        public const int MAXIMO_QUANTIDADE_ITEM = 999;
        public const int TAMANHO_MAXIMO_OBSERVACAO = 500;

        public CriarPedidoValidator()
        {
            RuleFor(request => request.Itens).NotEmpty().WithMessage(ResourceMensagensDeErro.PEDIDO_SEM_ITENS);
            RuleFor(request => request.Itens.Count).LessThanOrEqualTo(MAXIMO_ITENS)
                .When(request => request.Itens is not null)
                .WithMessage(ResourceMensagensDeErro.PEDIDO_MUITOS_ITENS);

            RuleForEach(request => request.Itens).ChildRules(item =>
            {
                item.RuleFor(i => i.Quantidade).InclusiveBetween(1, MAXIMO_QUANTIDADE_ITEM)
                    .WithMessage(ResourceMensagensDeErro.QUANTIDADE_ITEM_INVALIDA);
                item.RuleFor(i => i.PersonalizacaoIds)
                    .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
                    .WithMessage(ResourceMensagensDeErro.PERSONALIZACAO_REPETIDA);
            });

            RuleFor(request => request.Observacao).MaximumLength(TAMANHO_MAXIMO_OBSERVACAO)
                .WithMessage(ResourceMensagensDeErro.OBSERVACAO_MUITO_LONGA);
        }
    }
}
