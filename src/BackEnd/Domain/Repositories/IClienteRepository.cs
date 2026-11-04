using Domain.Entities;

namespace Domain.Repositories
{
    public interface IClienteRepository
    {  
        public Task Adicionar(Clientes clientes);

        public Task<bool> ExisteClienteComEmail(string email);

        // rastreado: o login regrava o hash quando a senha ainda está no formato antigo
        public Task<Clientes?> GetByEmail(string email);

        public Task<Clientes> GetById(long id);

        public void Update(Clientes clientes);
    }
}
