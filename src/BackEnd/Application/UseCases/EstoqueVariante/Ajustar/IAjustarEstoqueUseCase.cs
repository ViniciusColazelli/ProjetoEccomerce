using Application.Request;
using Application.Response;

namespace Application.UseCases.EstoqueVariante.Ajustar
{
    public interface IAjustarEstoqueUseCase
    {
        public Task<ResponseEstoqueVariante> Execute(int varianteId, RequestAjustarEstoque request);
    }
}
