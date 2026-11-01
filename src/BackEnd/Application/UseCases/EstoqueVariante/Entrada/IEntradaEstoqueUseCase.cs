using Application.Request;
using Application.Response;

namespace Application.UseCases.EstoqueVariante.Entrada
{
    public interface IEntradaEstoqueUseCase
    {
        public Task<ResponseEstoqueVariante> Execute(int varianteId, RequestMovimentarEstoque request);
    }
}
