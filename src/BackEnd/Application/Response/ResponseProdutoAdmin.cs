namespace Application.Response
{
    public class ResponseProdutoAdmin
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal PrecoBase { get; set; }
        public bool Ativo { get; set; }
        public IList<ResponseProdutoOpcaoAdmin> Opcoes { get; set; } = new List<ResponseProdutoOpcaoAdmin>();
        public IList<ResponseProdutoVarianteAdmin> Variantes { get; set; } = new List<ResponseProdutoVarianteAdmin>();
        public IList<ResponseProdutoImagem> Imagens { get; set; } = new List<ResponseProdutoImagem>(); // a primeira é a capa
    }
}
