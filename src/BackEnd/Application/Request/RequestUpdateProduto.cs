namespace Application.Request
{
    public class RequestUpdateProduto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal PrecoBase { get; set; }
    }
}
