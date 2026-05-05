namespace Exceptions.ExceptionBase
{
    public class ClienteNaoLogadoException : EccomerceException
    {
        public ClienteNaoLogadoException() : base(ResourceMensagensDeErro.CLIENTE_NAO_LOGADO){ }
    }
}
