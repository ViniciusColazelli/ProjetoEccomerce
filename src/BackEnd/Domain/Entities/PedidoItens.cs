namespace Domain.Entities
{
    // PrecoUnitario = preço base do produto + adicionais das personalizações, copiado no momento da compra.
    // O subtotal do item é PrecoUnitario × Quantidade.
    public class PedidoItens
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public int ProdutoId { get; set; }

        // a coluna aceita nulo por causa de pedidos anteriores às variantes; a API sempre preenche
        public int? VarianteId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public string? LogoUrl { get; set; }

        public Produtos Produto { get; set; } = null!;
        public ProdutoVariantes? Variante { get; set; }
        public ICollection<PedidoItemCustomizacoes> Customizacoes { get; set; } = new List<PedidoItemCustomizacoes>();
    }
}
