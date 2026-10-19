namespace Application.UseCases.ProdutoOpcao.Remover
{
    public interface IRemoverOpcaoUseCase
    {
        public Task Execute(int produtoId, int opcaoId);
    }
}
