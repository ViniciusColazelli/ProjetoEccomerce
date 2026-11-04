using Domain.Entities;
using Domain.Filtros;

namespace Domain.Repositories
{
    public interface IProdutoVarianteRepository
    {
        public Task<ProdutoVariantes?> GetById(int produtoId, int varianteId);

        public Task<bool> Existe(int varianteId);

        public Task<IList<ProdutoVariantes>> ListarPorProduto(int produtoId);

        public Task<IList<ProdutoVariantes>> ListarComEstoque(FiltroListagemEstoque filtro);

        public Task<int> ContarComEstoque(FiltroListagemEstoque filtro);

        public Task<ProdutoVariantes?> ObterComEstoque(int varianteId);

        public Task<IList<ProdutoVariantes>> ListarComEstoquePorIds(IList<int> varianteIds);

        public Task Adicionar(IList<ProdutoVariantes> variantes);

        public void Update(ProdutoVariantes variante);
    }
}
