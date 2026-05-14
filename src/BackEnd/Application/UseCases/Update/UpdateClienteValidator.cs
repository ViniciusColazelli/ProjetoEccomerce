using Application.Request;
using Domain.Extensions;
using Exceptions;
using FluentValidation;

namespace Application.UseCases.Update
{
    public class UpdateClienteValidator : AbstractValidator<RequestUpdateCliente>
    {
        public UpdateClienteValidator()
        {
            RuleFor(request => request.Nome).NotEmpty().WithMessage(ResourceMensagensDeErro.NOME_VAZIO);
            RuleFor(request => request.Email).NotEmpty().WithMessage(ResourceMensagensDeErro.EMAIL_VAZIO);
            When(request => string.IsNullOrEmpty(request.Email).IsFalse(), () =>
            {
                RuleFor(request => request.Email).EmailAddress().WithMessage(ResourceMensagensDeErro.EMAIL_INVALIDO);
            });
        }
    }
}
