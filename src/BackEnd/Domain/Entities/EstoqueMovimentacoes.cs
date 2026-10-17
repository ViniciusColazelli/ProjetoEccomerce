using Domain.Enums;

namespace Domain.Entities
{
    // Histórico de entradas e saídas. A quantidade é sempre positiva; o tipo diz a direção.
    public class EstoqueMovimentacoes
    {
        public long Id { get; set; }
        public int VarianteId { get; set; }
        public TipoMovimentacao Tipo { get; set; }
        public int Quantidade { get; set; }
        public string? Motivo { get; set; }
        public int? PedidoId { get; set; }
        public DateTime CriadoEm { get; set; }

        public ProdutoVariantes Variante { get; set; } = null!;
    }
}
