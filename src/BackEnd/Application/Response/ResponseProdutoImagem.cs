namespace Application.Response
{
    public class ResponseProdutoImagem
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;          // versão grande (página do produto)
        public string UrlMiniatura { get; set; } = string.Empty; // cards, busca e carrinho
        public int Ordem { get; set; }                           // a menor é a capa
    }
}
