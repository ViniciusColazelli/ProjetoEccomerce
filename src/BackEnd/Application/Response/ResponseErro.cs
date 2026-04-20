namespace Application.Response
{
    public class ResponseErro
    {
        public IList<string> Errors { get; set; }

        public ResponseErro(IList<string> errors)
        {
            Errors = errors;
        }

        public ResponseErro(string errors)
        {
            Errors = new List<string> 
            { 
                errors
            };
        }
    }
}
