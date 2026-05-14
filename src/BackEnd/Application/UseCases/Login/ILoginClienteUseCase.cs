using Application.Request;
using Application.Response;

namespace Application.UseCases.Login
{
    public interface ILoginClienteUseCase
    {
        public Task<ResponseClienteRegistrado> Execute(RequestLoginCliente request);
    }
}
