namespace Application.Request
{
    public class RequestListarMeusPedidos
    {
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 10;
    }
}
