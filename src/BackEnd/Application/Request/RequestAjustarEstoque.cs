namespace Application.Request
{
    public class RequestAjustarEstoque
    {
        public int NovaQuantidade { get; set; } // saldo contado no inventário
        public string Motivo { get; set; } = string.Empty;
    }
}
