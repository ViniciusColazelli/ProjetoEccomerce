using Exceptions;
using FluentValidation;
using FluentValidation.Validators;
using System.Text;

namespace Application.SharedValidators
{
    public class SenhaValidator<T> : PropertyValidator<T, string>  // validator customizado para validar senhas
    {
        public const int TAMANHO_MAXIMO_BYTES = 72;

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

            // o BCrypt só considera os primeiros 72 bytes: o que passasse disso seria ignorado sem aviso
            if (Encoding.UTF8.GetByteCount(senha) > TAMANHO_MAXIMO_BYTES)
            {
                context.MessageFormatter.AppendArgument("ErrorMessage", ResourceMensagensDeErro.SENHA_MUITO_LONGA);

                return false;
            }

            return true;
        }

        public override string Name => "SenhaValidator"; // Nome do validator

        protected override string GetDefaultMessageTemplate(string errorCode) => "{ErrorMessage}";

    }
}
