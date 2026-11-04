namespace Application.Request
{
    public class RequestAlterarStatusPedido
    {
        public string Status { get; set; } = string.Empty; // AguardandoPagamento | Pago | EmProducao | ProntoParaRetirada | Entregue | Cancelado
    }
}
