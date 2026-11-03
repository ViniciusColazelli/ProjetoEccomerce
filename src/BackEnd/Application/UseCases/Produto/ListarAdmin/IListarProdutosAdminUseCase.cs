using Application.Request;
using Application.Response;

namespace Application.UseCases.Produto.ListarAdmin
{
    public interface IListarProdutosAdminUseCase
    {
        public Task<ResponseProdutosAdmin> Execute(RequestListarProdutosAdmin request);
    }
}
