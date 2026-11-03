using Application.Request;
using Application.Response;

namespace Application.UseCases.EstoqueVariante.Listar
{
    public interface IListarEstoqueUseCase
    {
        public Task<ResponseEstoque> Execute(RequestListarEstoque request);
    }
}
