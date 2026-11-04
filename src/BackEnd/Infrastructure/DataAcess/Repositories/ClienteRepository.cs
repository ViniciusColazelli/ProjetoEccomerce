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
            // e-mail não diferencia maiúsculas: "Ana@x.com" e "ana@x.com" são a mesma conta
            var emailNormalizado = email.Trim().ToLower();

            return await _dbcontext.Clientes.AnyAsync(c => c.Email.ToLower() == emailNormalizado);
        }

        public async Task<Clientes> GetById(long id)
        {
            return await _dbcontext.Clientes.FirstAsync(user => user.Id == id);
        }

        public async Task<Clientes?> GetByEmail(string email)
        {
            // "email": null no JSON do login chega aqui como nulo
            var emailNormalizado = (email ?? string.Empty).Trim().ToLower();

            return await _dbcontext.Clientes.FirstOrDefaultAsync(user => user.Email.ToLower() == emailNormalizado);
        }

        public void Update(Clientes clientes)
        {
            _dbcontext.Clientes.Update(clientes);
        }
    }
}
