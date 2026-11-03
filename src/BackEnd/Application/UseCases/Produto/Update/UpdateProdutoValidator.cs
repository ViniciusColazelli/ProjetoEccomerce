using Application.Request;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.Produto.Update
{
    public class UpdateProdutoValidator : AbstractValidator<RequestUpdateProduto>
    {
        public const int TAMANHO_MAXIMO_NOME = 200;
        public const int TAMANHO_MAXIMO_DESCRICAO = 1000;

        public UpdateProdutoValidator()
        {
            RuleFor(produto => produto.Nome).NotEmpty().WithMessage(ResourceMensagensDeErro.NOME_VAZIO);
            RuleFor(produto => produto.Nome).MaximumLength(TAMANHO_MAXIMO_NOME).WithMessage(ResourceMensagensDeErro.NOME_MUITO_LONGO);
            RuleFor(produto => produto.Descricao).MaximumLength(TAMANHO_MAXIMO_DESCRICAO).WithMessage(ResourceMensagensDeErro.DESCRICAO_MUITO_LONGA);
            RuleFor(produto => produto.PrecoBase).GreaterThan(0).WithMessage(ResourceMensagensDeErro.PRECO_INVALIDO);
            RuleFor(produto => produto.PrecoBase).PrecisionScale(10, 2, true).WithMessage(ResourceMensagensDeErro.PRECO_FORMATO_INVALIDO);
        }
    }
}
