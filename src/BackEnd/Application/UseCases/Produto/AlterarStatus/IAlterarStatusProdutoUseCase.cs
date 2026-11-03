using Application.Request;

namespace Application.UseCases.Produto.AlterarStatus
{
    public interface IAlterarStatusProdutoUseCase
    {
        public Task Execute(int id, RequestAlterarStatus request);
    }
}
