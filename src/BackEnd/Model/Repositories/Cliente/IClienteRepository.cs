using Domain.Entities;

namespace Domain.Repositories.Cliente
{
    public interface IClienteRepository
    {
        public Task Adicionar(Clientes clientes);

        public Task<bool> ExisteClienteComEmail(string email);
    }
}
