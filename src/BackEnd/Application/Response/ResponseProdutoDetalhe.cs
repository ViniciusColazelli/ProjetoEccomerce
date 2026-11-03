namespace Application.Response
{
    public class ResponseProdutoDetalhe
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal PrecoBase { get; set; }
        public bool Disponivel { get; set; }
        public IList<ResponseProdutoOpcao> Tamanhos { get; set; } = new List<ResponseProdutoOpcao>();
        public IList<ResponseProdutoOpcao> Cores { get; set; } = new List<ResponseProdutoOpcao>();
        public IList<ResponseProdutoOpcao> Personalizacoes { get; set; } = new List<ResponseProdutoOpcao>();
        public IList<ResponseProdutoVariante> Variantes { get; set; } = new List<ResponseProdutoVariante>();
    }
}
