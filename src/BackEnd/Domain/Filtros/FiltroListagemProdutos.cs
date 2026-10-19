using Domain.Enums;

namespace Domain.Filtros
{
    public class FiltroListagemProdutos
    {
        public string? Nome { get; set; }
        public OrdenacaoProduto OrdenarPor { get; set; } = OrdenacaoProduto.Nome;
        public bool Decrescente { get; set; }
        public bool SomenteDisponiveis { get; set; }
        public bool? Ativo { get; set; } // só na listagem do admin; null = todos
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
    }
}
