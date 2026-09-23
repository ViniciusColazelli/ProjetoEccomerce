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

        public async Task<Clientes> GetById(long id)
        {
            return await _dbcontext.Clientes.FirstAsync(user => user.Id == id);
        }

        public async Task<Clientes?> GetEmailAndPassword(string email, string senha)
        {
            return await _dbcontext.Clientes.AsNoTracking().FirstOrDefaultAsync(user => user.Email.Equals(email) && user.Senha.Equals(senha));
        }

        public void Update(Clientes clientes)
        {
            _dbcontext.Clientes.Update(clientes);
        }
    }
}
