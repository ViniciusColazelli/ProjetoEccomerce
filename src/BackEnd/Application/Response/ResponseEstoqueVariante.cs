namespace Application.Response
{
    public class ResponseEstoqueVariante
    {
        public int VarianteId { get; set; }
        public int ProdutoId { get; set; }
        public string Produto { get; set; } = string.Empty;
        public string Tamanho { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public bool Ativo { get; set; }
        public int Quantidade { get; set; }
    }
}
