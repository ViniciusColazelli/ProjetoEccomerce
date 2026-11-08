namespace Application.UseCases.ProdutoImagem.Remover
{
    public interface IRemoverImagemUseCase
    {
        public Task Execute(int produtoId, int imagemId);
    }
}
