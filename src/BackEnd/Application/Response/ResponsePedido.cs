using Domain.Enums;

namespace Application.Response
{
    public class ResponsePedido
    {
        public int Id { get; set; }
        public StatusPedido Status { get; set; }
        public decimal Total { get; set; }
        public string? Observacao { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
        public IList<ResponsePedidoItem> Itens { get; set; } = new List<ResponsePedidoItem>();
    }

    public class ResponsePedidoItem
    {
        public int Id { get; set; }
        public int ProdutoId { get; set; }
        public string Produto { get; set; } = string.Empty;
        public int? VarianteId { get; set; }
        public string? Tamanho { get; set; }
        public string? Cor { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }      // preço base + personalizações, no momento da compra
        public decimal Subtotal { get; set; }           // PrecoUnitario × Quantidade
        public IList<ResponsePedidoPersonalizacao> Personalizacoes { get; set; } = new List<ResponsePedidoPersonalizacao>();
    }

    public class ResponsePedidoPersonalizacao
    {
        public int OpcaoId { get; set; }
        public string Valor { get; set; } = string.Empty;
        public decimal PrecoAdicional { get; set; }
    }
}
