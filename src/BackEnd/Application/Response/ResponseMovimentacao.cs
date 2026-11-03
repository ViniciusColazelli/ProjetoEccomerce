using Domain.Enums;

namespace Application.Response
{
    public class ResponseMovimentacao
    {
        public long Id { get; set; }
        public int VarianteId { get; set; }
        public int ProdutoId { get; set; }
        public string Produto { get; set; } = string.Empty;
        public string Tamanho { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public TipoMovimentacao Tipo { get; set; }
        public int Quantidade { get; set; }
        public string? Motivo { get; set; }
        public int? PedidoId { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
