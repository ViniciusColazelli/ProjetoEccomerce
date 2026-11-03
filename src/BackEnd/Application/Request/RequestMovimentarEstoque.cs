namespace Application.Request
{
    public class RequestMovimentarEstoque
    {
        public int Quantidade { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
