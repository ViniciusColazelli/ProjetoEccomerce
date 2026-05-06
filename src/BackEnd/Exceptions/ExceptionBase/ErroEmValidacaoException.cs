namespace Exceptions.ExceptionBase
{
    public class ErroEmValidacaoException : EccomerceException
    {
        public IList<string> ErrorMessages { get; set; }

        public ErroEmValidacaoException(IList<string> errorMessages) : base(string.Empty)
        {
            ErrorMessages = errorMessages;
        }
    }
}
