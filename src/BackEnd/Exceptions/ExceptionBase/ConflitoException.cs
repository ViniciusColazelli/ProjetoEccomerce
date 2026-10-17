namespace Exceptions.ExceptionBase
{
    // 409: a requisição é válida, mas conflita com o estado atual (ex.: estoque insuficiente, opção em uso)
    public class ConflitoException : EccomerceException
    {
        public ConflitoException(string message) : base(message) { }
    }
}
