using Application.Request;
using Application.Response;

namespace Application.UseCases.EstoqueVariante.Saida
{
    public interface ISaidaEstoqueUseCase
    {
        public Task<ResponseEstoqueVariante> Execute(int varianteId, RequestMovimentarEstoque request);
    }
}
