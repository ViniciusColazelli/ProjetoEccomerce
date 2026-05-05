using Domain.Entities;

namespace Domain.Repositories
{
    public interface IClienteRepository
    {  
        public Task Adicionar(Clientes clientes);

        public Task<bool> ExisteClienteComEmail(string email);

        public Task<Clientes?> GetEmailAndPassword(string email, string senha);

        public Task<Clientes> GetById(long id);

        public void Update(Clientes clientes);
    }
}
