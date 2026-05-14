using Exceptions;
using FluentValidation;
using FluentValidation.Validators;

namespace Application.SharedValidators
{
    public class SenhaValidator<T> : PropertyValidator<T, string>  // validator customizado para validar senhas
    {
        public override bool IsValid(ValidationContext<T> context, string senha)
        {
            if (string.IsNullOrWhiteSpace(senha))
            {
                context.MessageFormatter.AppendArgument("ErrorMessage", ResourceMensagensDeErro.SENHA_VAZIO);

                return false;
            }

            if (senha.Length < 6)
            {
                context.MessageFormatter.AppendArgument("ErrorMessage", ResourceMensagensDeErro.SENHA_INVALIDA);

                return false;
            }

            return true;
        }

        public override string Name => "SenhaValidator"; // Nome do validator

        protected override string GetDefaultMessageTemplate(string errorCode) => "{ErrorMessage}";

    }
}
