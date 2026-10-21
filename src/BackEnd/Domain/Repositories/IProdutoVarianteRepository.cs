using Domain.Entities;

namespace Domain.Repositories
{
    public interface IProdutoVarianteRepository
    {
        public Task<ProdutoVariantes?> GetById(int produtoId, int varianteId);

        public Task<IList<ProdutoVariantes>> ListarPorProduto(int produtoId);

        public Task Adicionar(IList<ProdutoVariantes> variantes);

        public void Update(ProdutoVariantes variante);
    }
}
