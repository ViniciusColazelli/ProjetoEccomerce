using Application.Request;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.Registrar
{
    public class RegistrarClienteValidator : AbstractValidator<RequestRegistrarCliente>
    {
        public RegistrarClienteValidator()
        {
            RuleFor(cliente => cliente.Nome).NotEmpty().WithMessage(ResourceMensagensDeErro.NOME_VAZIO);
            RuleFor(cliente => cliente.Email).NotEmpty().WithMessage(ResourceMensagensDeErro.EMAIL_VAZIO);
            RuleFor(cliente => cliente.Email).EmailAddress().WithMessage(ResourceMensagensDeErro.EMAIL_INVALIDO);
            RuleFor(cliente => cliente.Senha).NotEmpty().WithMessage(ResourceMensagensDeErro.SENHA_VAZIO);
            RuleFor(cliente => cliente.Senha).MinimumLength(6).WithMessage(ResourceMensagensDeErro.SENHA_INVALIDA);

        }
    }
}
