namespace Application.Response
{
    public class ResponseProdutoVarianteAdmin
    {
        public int Id { get; set; }
        public int TamanhoId { get; set; }
        public string Tamanho { get; set; } = string.Empty;
        public int? CorId { get; set; }
        public string? Cor { get; set; }
        public bool Ativo { get; set; }
        public int Quantidade { get; set; }
    }
}
