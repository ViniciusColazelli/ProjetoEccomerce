using Application.Request;

namespace Application.UseCases.Produto.Update
{
    public interface IUpdateProdutoUseCase
    {
        public Task Execute(int id, RequestUpdateProduto request);
    }
}
