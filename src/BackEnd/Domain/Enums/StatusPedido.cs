namespace Domain.Enums
{
    // Gravado como texto em pedidos.status: os nomes precisam bater com o CHECK da migration 14.
    // AguardandoPagamento → Pago → EmProducao → ProntoParaRetirada → Entregue; Cancelado só antes do pagamento.
    public enum StatusPedido
    {
        AguardandoPagamento,
        Pago,
        EmProducao,
        ProntoParaRetirada,
        Entregue,
        Cancelado
    }
}
