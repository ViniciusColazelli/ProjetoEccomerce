namespace Application.Response
{
    public class ResponseEstoque
    {
        public IList<ResponseEstoqueVariante> Variantes { get; set; } = new List<ResponseEstoqueVariante>();
        public int Pagina { get; set; }
        public int TamanhoPagina { get; set; }
        public int Total { get; set; }
    }
}
