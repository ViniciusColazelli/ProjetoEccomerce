namespace Application.Request
{
    public class RequestListarMovimentacoes
    {
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
        public int? VarianteId { get; set; }
        public int? ProdutoId { get; set; }
        public int? PedidoId { get; set; }
        public string? Tipo { get; set; } // Entrada | Saida
        public DateOnly? De { get; set; }
        public DateOnly? Ate { get; set; }
    }
}
