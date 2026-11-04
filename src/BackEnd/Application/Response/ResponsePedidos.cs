using Domain.Enums;

namespace Application.Response
{
    public class ResponsePedidos
    {
        public IList<ResponsePedidoResumo> Pedidos { get; set; } = new List<ResponsePedidoResumo>();
        public int Pagina { get; set; }
        public int TamanhoPagina { get; set; }
        public int Total { get; set; }
    }

    public class ResponsePedidoResumo
    {
        public int Id { get; set; }
        public StatusPedido Status { get; set; }
        public decimal Total { get; set; }
        public int QuantidadePecas { get; set; }   // soma das quantidades dos itens
        public DateTime CriadoEm { get; set; }
    }
}
