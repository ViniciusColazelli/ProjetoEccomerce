namespace Domain.Entities
{
    // Combinação tamanho × cor de um produto. A cor é opcional (produto sem cor).
    public class ProdutoVariantes
    {
        public int Id { get; set; }
        public int ProdutoId { get; set; }
        public int TamanhoOpcaoId { get; set; }
        public int? CorOpcaoId { get; set; }
        public bool Ativo { get; set; } = true;

        public ProdutoOpcoes TamanhoOpcao { get; set; } = null!;
        public ProdutoOpcoes? CorOpcao { get; set; }
        public Estoque? Estoque { get; set; }
    }
}
