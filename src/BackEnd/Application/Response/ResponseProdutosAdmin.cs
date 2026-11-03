namespace Application.Response
{
    public class ResponseProdutosAdmin
    {
        public IList<ResponseProdutoResumoAdmin> Produtos { get; set; } = new List<ResponseProdutoResumoAdmin>();
        public int Pagina { get; set; }
        public int TamanhoPagina { get; set; }
        public int Total { get; set; }
    }
}
