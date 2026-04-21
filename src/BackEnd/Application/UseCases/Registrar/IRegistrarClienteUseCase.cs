using Application.Request;
using Application.Response;

namespace Application.UseCases.Registrar
{
    public interface IRegistrarClienteUseCase
    {
        public Task<ResponseClienteRegistrado> Execute(RequestRegistrarCliente request);
    }
}
