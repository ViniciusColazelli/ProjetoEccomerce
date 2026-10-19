namespace Application.Request
{
    public class RequestListarProdutos
    {
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
        public string? Nome { get; set; }
        public string? OrdenarPor { get; set; } // nome | preco
        public string? Direcao { get; set; } // asc | desc
        public bool SomenteDisponiveis { get; set; }
    }
}
