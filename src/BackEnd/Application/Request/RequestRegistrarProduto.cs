namespace Application.Request
{
    public class RequestRegistrarProduto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal PrecoBase { get; set; }
    }
}
