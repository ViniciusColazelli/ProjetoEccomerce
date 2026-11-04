namespace Application.Response
{
    public class ResponsePedidosAdmin
    {
        public IList<ResponsePedidoResumoAdmin> Pedidos { get; set; } = new List<ResponsePedidoResumoAdmin>();
        public int Pagina { get; set; }
        public int TamanhoPagina { get; set; }
        public int Total { get; set; }
    }

    public class ResponsePedidoResumoAdmin : ResponsePedidoResumo
    {
        public DateTime? AtualizadoEm { get; set; }
        public ResponsePedidoCliente Cliente { get; set; } = new();
    }

    public class ResponsePedidoAdmin : ResponsePedido
    {
        public ResponsePedidoCliente Cliente { get; set; } = new();
    }

    public class ResponsePedidoCliente
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
