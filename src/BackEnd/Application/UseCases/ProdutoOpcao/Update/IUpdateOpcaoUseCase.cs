using Application.Request;

namespace Application.UseCases.ProdutoOpcao.Update
{
    public interface IUpdateOpcaoUseCase
    {
        public Task Execute(int produtoId, int opcaoId, RequestUpdateOpcao request);
    }
}
