namespace Domain.Filtros
{
    public class FiltroListagemEstoque
    {
        public int? ProdutoId { get; set; }
        public bool SomenteZerados { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
    }
}
