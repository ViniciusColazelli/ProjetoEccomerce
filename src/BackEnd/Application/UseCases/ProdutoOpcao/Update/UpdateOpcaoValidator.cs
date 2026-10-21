using Application.Request;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.ProdutoOpcao.Update
{
    public class UpdateOpcaoValidator : AbstractValidator<RequestUpdateOpcao>
    {
        public const int TAMANHO_MAXIMO_VALOR = 200;

        public UpdateOpcaoValidator()
        {
            RuleFor(opcao => opcao.Valor).NotEmpty().WithMessage(ResourceMensagensDeErro.VALOR_OPCAO_VAZIO);
            RuleFor(opcao => opcao.Valor).MaximumLength(TAMANHO_MAXIMO_VALOR).WithMessage(ResourceMensagensDeErro.VALOR_OPCAO_MUITO_LONGO);
            RuleFor(opcao => opcao.PrecoAdicional).GreaterThanOrEqualTo(0).WithMessage(ResourceMensagensDeErro.PRECO_ADICIONAL_INVALIDO);
            RuleFor(opcao => opcao.PrecoAdicional).PrecisionScale(10, 2, true).WithMessage(ResourceMensagensDeErro.PRECO_FORMATO_INVALIDO);
        }
    }
}
