using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public ClienteRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task Adicionar(Clientes clientes)
        {
            await _dbcontext.clientes.AddAsync(clientes);
        }

        public async Task<bool> ExisteClienteComEmail(string email) 
        {
            return await _dbcontext.clientes.AnyAsync(c => c.Email.Equals(email));
        }

    }
}
