using Domain.Enums;

namespace Domain.Filtros
{
    public class FiltroListagemMovimentacoes
    {
        public int? VarianteId { get; set; }
        public int? ProdutoId { get; set; }
        public int? PedidoId { get; set; }
        public TipoMovimentacao? Tipo { get; set; }
        public DateOnly? De { get; set; }
        public DateOnly? Ate { get; set; } // inclusivo: vale o dia inteiro
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
    }
}
