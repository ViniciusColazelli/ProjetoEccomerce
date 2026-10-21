using Application.Request;

namespace Application.UseCases.ProdutoOpcao.AlterarStatus
{
    public interface IAlterarStatusOpcaoUseCase
    {
        public Task Execute(int produtoId, int opcaoId, RequestAlterarStatus request);
    }
}
