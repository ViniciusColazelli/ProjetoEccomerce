using Application.Request;
using Application.Response;

namespace Application.UseCases.Produto.Listar
{
    public interface IListarProdutosUseCase
    {
        public Task<ResponseProdutos> Execute(RequestListarProdutos request);
    }
}
