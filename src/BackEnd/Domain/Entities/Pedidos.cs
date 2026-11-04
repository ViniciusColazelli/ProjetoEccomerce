using Domain.Enums;

namespace Domain.Entities
{
    // O total é a soma dos itens no momento da compra: mudar o preço do produto depois não altera o pedido.
    public class Pedidos
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public StatusPedido Status { get; set; } = StatusPedido.AguardandoPagamento;
        public decimal Total { get; set; }
        public string? Observacao { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }

        public Clientes Cliente { get; set; } = null!;
        public ICollection<PedidoItens> Itens { get; set; } = new List<PedidoItens>();
    }
}
