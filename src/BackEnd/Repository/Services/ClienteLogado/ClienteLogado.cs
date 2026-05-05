using Domain.Entities;
using Domain.Services.ClienteLogado;

namespace Infrastructure.Services.ClienteLogado
{
    public class ClienteLogado : IClienteLogado
    {
        public Task<Clientes> Cliente()
        {
            throw new NotImplementedException();
        }
    }
}
