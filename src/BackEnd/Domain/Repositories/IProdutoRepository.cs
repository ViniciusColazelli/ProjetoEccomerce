using Domain.Entities;
using Domain.Filtros;

namespace Domain.Repositories
{
    public interface IProdutoRepository
    {
        public Task<IList<Produtos>> ListarAtivos(FiltroListagemProdutos filtro);

        public Task<int> ContarAtivos(FiltroListagemProdutos filtro);

        public Task<Produtos?> ObterAtivoComDetalhes(int id);
    }
}
