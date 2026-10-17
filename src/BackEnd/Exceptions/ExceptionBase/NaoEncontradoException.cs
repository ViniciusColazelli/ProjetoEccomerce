namespace Exceptions.ExceptionBase
{
    // 404: o recurso pedido não existe (ou não está visível para quem pediu)
    public class NaoEncontradoException : EccomerceException
    {
        public NaoEncontradoException(string message) : base(message) { }
    }
}
