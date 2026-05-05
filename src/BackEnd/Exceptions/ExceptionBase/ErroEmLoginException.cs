namespace Exceptions.ExceptionBase
{
    public class ErroEmLoginException : EccomerceException
    {
        public ErroEmLoginException() : base(ResourceMensagensDeErro.EMAIL_OU_SENHA_INVALIDO)
        {
        }
    }
}
