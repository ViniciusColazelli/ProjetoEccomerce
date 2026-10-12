namespace Application.Request
{
    public class RequestListarProdutos
    {
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
    }
}
