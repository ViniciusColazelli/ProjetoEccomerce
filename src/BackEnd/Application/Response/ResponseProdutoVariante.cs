namespace Application.Response
{
    public class ResponseProdutoVariante
    {
        public int Id { get; set; }
        public int TamanhoId { get; set; }
        public int? CorId { get; set; }
        public bool Disponivel { get; set; } // o público não vê a quantidade
    }
}
