using Domain.Enums;

namespace Application.Response
{
    public class ResponseProdutoOpcaoAdmin
    {
        public int Id { get; set; }
        public TipoOpcao Tipo { get; set; }
        public string Valor { get; set; } = string.Empty;
        public decimal PrecoAdicional { get; set; }
        public bool Ativo { get; set; }
    }
}
