namespace Application.Response
{
    public class ResponseProdutos
    {
        public IList<ResponseProdutoResumo> Produtos { get; set; } = new List<ResponseProdutoResumo>();
        public int Pagina { get; set; }
        public int TamanhoPagina { get; set; }
        public int Total { get; set; }
    }
}
