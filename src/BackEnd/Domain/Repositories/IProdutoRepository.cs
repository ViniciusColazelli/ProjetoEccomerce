using Domain.Entities;
using Domain.Filtros;

namespace Domain.Repositories
{
    public interface IProdutoRepository
    {
        public Task<IList<Produtos>> ListarAtivos(FiltroListagemProdutos filtro);

        public Task<int> ContarAtivos(FiltroListagemProdutos filtro);

        public Task<Produtos?> ObterAtivoComDetalhes(int id);

        public Task<IList<Produtos>> ListarTodos(FiltroListagemProdutos filtro);

        public Task<int> ContarTodos(FiltroListagemProdutos filtro);

        public Task<Produtos?> ObterComDetalhes(int id);

        public Task<Produtos?> GetById(int id);

        public Task<bool> Existe(int id);

        public Task Adicionar(Produtos produto);

        public void Update(Produtos produto);
    }
}
