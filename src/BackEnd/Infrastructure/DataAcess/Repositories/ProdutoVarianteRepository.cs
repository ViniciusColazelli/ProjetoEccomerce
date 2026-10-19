using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class ProdutoVarianteRepository : IProdutoVarianteRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public ProdutoVarianteRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task<ProdutoVariantes?> GetById(int produtoId, int varianteId)
        {
            return await _dbcontext.ProdutoVariantes.FirstOrDefaultAsync(variante => variante.Id == varianteId && variante.ProdutoId == produtoId);
        }

        public async Task<IList<ProdutoVariantes>> ListarPorProduto(int produtoId)
        {
            return await _dbcontext.ProdutoVariantes
                .AsNoTracking()
                .Where(variante => variante.ProdutoId == produtoId)
                .ToListAsync();
        }

        public async Task Adicionar(IList<ProdutoVariantes> variantes)
        {
            await _dbcontext.ProdutoVariantes.AddRangeAsync(variantes);
        }

        public void Update(ProdutoVariantes variante)
        {
            _dbcontext.ProdutoVariantes.Update(variante);
        }
    }
}
