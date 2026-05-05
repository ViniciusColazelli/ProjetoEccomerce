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
            await _dbcontext.Clientes.AddAsync(clientes);
        }

        public async Task<bool> ExisteClienteComEmail(string email) 
        {
            return await _dbcontext.Clientes.AnyAsync(c => c.Email.Equals(email));
        }

        public Task<Clientes> GetById(long id)
        {
            throw new NotImplementedException();
        }

        public Task<Clientes?> GetEmailAndPassword(string email, string senha)
        {
            throw new NotImplementedException();
        }

        public void Update(Clientes clientes)
        {
            throw new NotImplementedException();
        }
    }
}
