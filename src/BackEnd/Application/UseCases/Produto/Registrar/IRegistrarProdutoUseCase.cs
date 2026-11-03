using Application.Request;
using Application.Response;

namespace Application.UseCases.Produto.Registrar
{
    public interface IRegistrarProdutoUseCase
    {
        public Task<ResponseProdutoRegistrado> Execute(RequestRegistrarProduto request);
    }
}
