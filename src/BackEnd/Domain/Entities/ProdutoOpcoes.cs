using Domain.Enums;

namespace Domain.Entities
{
    public class ProdutoOpcoes
    {
        public int Id { get; set; }
        public int ProdutoId { get; set; }
        public TipoOpcao Tipo { get; set; }
        public string Valor { get; set; } = string.Empty;
        public decimal PrecoAdicional { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
