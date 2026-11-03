using Application.Response;

namespace Application.UseCases.EstoqueVariante.Obter
{
    public interface IObterEstoqueUseCase
    {
        public Task<ResponseEstoqueVariante> Execute(int varianteId);
    }
}
