using Domain.Entities;

namespace Domain.Services.ClienteLogado
{
    public interface IClienteLogado
    {
        public Task<Clientes> Cliente();
    }
}
