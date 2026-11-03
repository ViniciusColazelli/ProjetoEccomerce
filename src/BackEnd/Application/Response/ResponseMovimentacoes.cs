namespace Application.Response
{
    public class ResponseMovimentacoes
    {
        public IList<ResponseMovimentacao> Movimentacoes { get; set; } = new List<ResponseMovimentacao>();
        public int Pagina { get; set; }
        public int TamanhoPagina { get; set; }
        public int Total { get; set; }
    }
}
