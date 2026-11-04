namespace Exceptions.ExceptionBase
{
    // 409: a requisição é válida, mas conflita com o estado atual (ex.: estoque insuficiente, opção em uso)
    public class ConflitoException : EccomerceException
    {
        public IList<string> ErrorMessages { get; }

        public ConflitoException(string message) : base(message)
        {
            ErrorMessages = new List<string> { message };
        }

        // vários conflitos de uma vez (ex.: mais de uma peça do pedido sem estoque)
        public ConflitoException(IList<string> messages) : base(messages.FirstOrDefault() ?? string.Empty)
        {
            ErrorMessages = messages;
        }
    }
}
