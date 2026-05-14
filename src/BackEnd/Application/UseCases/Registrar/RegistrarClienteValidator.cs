using Application.Request;
using Application.SharedValidators;
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
            RuleFor(cliente => cliente.Senha).SetValidator(new SenhaValidator<RequestRegistrarCliente>());
        }
    }
}
