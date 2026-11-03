namespace Application.Request
{
    public class RequestListarEstoque
    {
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
        public int? ProdutoId { get; set; }
        public bool SomenteZerados { get; set; }
    }
}
