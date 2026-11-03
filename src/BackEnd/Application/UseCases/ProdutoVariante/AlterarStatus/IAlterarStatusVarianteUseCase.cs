using Application.Request;

namespace Application.UseCases.ProdutoVariante.AlterarStatus
{
    public interface IAlterarStatusVarianteUseCase
    {
        public Task Execute(int produtoId, int varianteId, RequestAlterarStatus request);
    }
}
