namespace Domain.Entities
{
    public class Produtos
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal PrecoBase { get; set; }
        public bool Ativo { get; set; }

        public ICollection<ProdutoOpcoes> Opcoes { get; set; } = new List<ProdutoOpcoes>();
        public ICollection<ProdutoVariantes> Variantes { get; set; } = new List<ProdutoVariantes>();
    }
}
