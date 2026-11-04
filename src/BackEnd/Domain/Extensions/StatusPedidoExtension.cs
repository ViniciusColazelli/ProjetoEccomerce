using Domain.Enums;

namespace Domain.Extensions
{
    // Ciclo de vida do pedido:
    //   AguardandoPagamento → Pago → EmProducao → ProntoParaRetirada → Entregue
    // EmProducao é opcional (peça sem personalização vai de Pago direto para ProntoParaRetirada).
    // Cancelado só antes do pagamento: depois de pago, o pedido segue até a entrega.
    // Entregue e Cancelado são finais.
    public static class StatusPedidoExtension
    {
        private static readonly IReadOnlyDictionary<StatusPedido, StatusPedido[]> Proximos = new Dictionary<StatusPedido, StatusPedido[]>
        {
            [StatusPedido.AguardandoPagamento] = [StatusPedido.Pago, StatusPedido.Cancelado],
            [StatusPedido.Pago] = [StatusPedido.EmProducao, StatusPedido.ProntoParaRetirada],
            [StatusPedido.EmProducao] = [StatusPedido.ProntoParaRetirada],
            [StatusPedido.ProntoParaRetirada] = [StatusPedido.Entregue],
            [StatusPedido.Entregue] = [],
            [StatusPedido.Cancelado] = []
        };

        public static bool PodeMudarPara(this StatusPedido atual, StatusPedido novo) => Proximos[atual].Contains(novo);
    }
}
