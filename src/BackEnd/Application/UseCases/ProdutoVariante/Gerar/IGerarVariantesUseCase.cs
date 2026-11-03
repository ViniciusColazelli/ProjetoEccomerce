using Application.Response;

namespace Application.UseCases.ProdutoVariante.Gerar
{
    public interface IGerarVariantesUseCase
    {
        public Task<ResponseVariantesGeradas> Execute(int produtoId);
    }
}
