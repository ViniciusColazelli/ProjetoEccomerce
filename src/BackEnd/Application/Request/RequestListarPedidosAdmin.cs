namespace Application.Request
{
    public class RequestListarPedidosAdmin
    {
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
        public string? Status { get; set; }   // AguardandoPagamento | Pago | EmProducao | ProntoParaRetirada | Entregue | Cancelado
        public string? Cliente { get; set; }  // parte do nome ou do e-mail
        public DateOnly? De { get; set; }
        public DateOnly? Ate { get; set; }
    }
}
