using Application.Request;
using Application.Response;

namespace Application.UseCases.Login
{
    public interface ILoginClienteUseCase
    {
        public Task<ResultadoLogin> Execute(RequestLoginCliente request);
    }
}
