using Application.Response;
using Domain.Services.ClienteLogado;

namespace Application.UseCases.Profile
{
    public class GetClienteProfileUseCase : IGetClienteProfileUseCase
    {
        private readonly IClienteLogado _clienteLogado;
        public GetClienteProfileUseCase(IClienteLogado clienteLogado)
        {
            _clienteLogado = clienteLogado;
        }
        public async Task<ResponseClienteProfile> Execute()
        {
            var user = await _clienteLogado.Cliente();

            return new ResponseClienteProfile()
            {
                Email = user.Email,
                Nome = user.Nome
            };
        }
    }
}
