using Application.Request;
using Application.Response;

namespace Application.UseCases.ProdutoOpcao.Registrar
{
    public interface IRegistrarOpcaoUseCase
    {
        public Task<ResponseProdutoOpcaoAdmin> Execute(int produtoId, RequestRegistrarOpcao request);
    }
}
