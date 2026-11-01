using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class EstoqueRepository : IEstoqueRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public EstoqueRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task<Estoque?> GetByVarianteId(int varianteId)
        {
            return await _dbcontext.Estoque.FirstOrDefaultAsync(estoque => estoque.VarianteId == varianteId);
        }

        public async Task Adicionar(Estoque estoque)
        {
            await _dbcontext.Estoque.AddAsync(estoque);
        }

        public void Update(Estoque estoque)
        {
            _dbcontext.Estoque.Update(estoque);
        }
    }
}
