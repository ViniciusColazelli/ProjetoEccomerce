namespace Application.Request
{
    public class RequestListarProdutosAdmin : RequestListarProdutos
    {
        public bool? Ativo { get; set; } // null = ativos e inativos
    }
}
