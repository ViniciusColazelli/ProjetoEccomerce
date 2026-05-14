using Application.Request;

namespace Application.UseCases.Update
{
    public interface IUpdateClienteUseCase
    {
        public Task Execute(RequestUpdateCliente request);
    }
}
