namespace Application.Request
{
    // O front manda só o que o cliente escolheu: preços e totais são calculados no servidor
    public class RequestCriarPedido
    {
        public List<RequestItemPedido> Itens { get; set; } = new();
        public string? Observacao { get; set; }
    }

    public class RequestItemPedido
    {
        public int VarianteId { get; set; }
        public int Quantidade { get; set; }
        public List<int> PersonalizacaoIds { get; set; } = new();
    }
}
