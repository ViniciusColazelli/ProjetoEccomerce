using Domain.Entities;

namespace Domain.Repositories
{
    public interface IProdutoRepository
    {
        public Task<IList<Produtos>> ListarAtivos(int pagina, int tamanhoPagina);

        public Task<int> ContarAtivos();
    }
}
