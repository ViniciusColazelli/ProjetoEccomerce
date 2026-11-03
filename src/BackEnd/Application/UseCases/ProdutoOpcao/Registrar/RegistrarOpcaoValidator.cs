using Application.Request;
using Domain.Enums;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.ProdutoOpcao.Registrar
{
    public class RegistrarOpcaoValidator : AbstractValidator<RequestRegistrarOpcao>
    {
        public const int TAMANHO_MAXIMO_VALOR = 200;

        public RegistrarOpcaoValidator()
        {
            RuleFor(opcao => opcao.Tipo)
                .Must(tipo => Enum.GetNames<TipoOpcao>().Contains(tipo, StringComparer.OrdinalIgnoreCase))
                .WithMessage(ResourceMensagensDeErro.TIPO_OPCAO_INVALIDO);
            RuleFor(opcao => opcao.Valor).NotEmpty().WithMessage(ResourceMensagensDeErro.VALOR_OPCAO_VAZIO);
            RuleFor(opcao => opcao.Valor).MaximumLength(TAMANHO_MAXIMO_VALOR).WithMessage(ResourceMensagensDeErro.VALOR_OPCAO_MUITO_LONGO);
            RuleFor(opcao => opcao.PrecoAdicional).GreaterThanOrEqualTo(0).WithMessage(ResourceMensagensDeErro.PRECO_ADICIONAL_INVALIDO);
            RuleFor(opcao => opcao.PrecoAdicional).PrecisionScale(10, 2, true).WithMessage(ResourceMensagensDeErro.PRECO_FORMATO_INVALIDO);
        }
    }
}
