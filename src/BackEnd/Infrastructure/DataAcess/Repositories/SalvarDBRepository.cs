using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class SalvarDBRepository : ISalvarDBRepository
    {
        private readonly EccomerceDbContext _dbContext;

        public SalvarDBRepository(EccomerceDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Salvar()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
