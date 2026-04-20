using Domain.Entities;

namespace Infrastructure.DataAcess.Repositories
{
    public class ClienteRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public ClienteRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task Adicionar(Clientes user)
        {
            await _dbcontext.clientes.AddAsync(user);
        }

        public async Task ExisteClienteAtivoComEmail(string email) 
        {
            
        }
    }
}
