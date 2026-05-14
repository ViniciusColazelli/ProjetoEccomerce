using Application.Request;
using Application.SharedValidators;
using FluentValidation;

namespace Application.UseCases.TrocarSenha
{
    public class TrocarSenhaValidator : AbstractValidator<RequestTrocarSenha>
    {
        public TrocarSenhaValidator()
        {
            RuleFor(x => x.NovaSenha).SetValidator(new SenhaValidator<RequestTrocarSenha>());
        }
    }
}
