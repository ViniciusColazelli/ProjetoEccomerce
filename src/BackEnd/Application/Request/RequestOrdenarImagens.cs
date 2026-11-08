namespace Application.Request
{
    // Todas as fotos do produto, na ordem nova: a primeira vira a capa
    public class RequestOrdenarImagens
    {
        public List<int> Ids { get; set; } = new();
    }
}
