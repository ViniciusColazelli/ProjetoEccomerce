namespace Application.Response
{
    public class ResponseProdutoResumoAdmin
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal PrecoBase { get; set; }
        public bool Ativo { get; set; }
        public string? ImagemUrl { get; set; } // miniatura da capa; null quando o produto não tem foto
    }
}
