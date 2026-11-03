using Application.Request;
using Application.Response;

namespace Application.UseCases.EstoqueVariante.Historico
{
    public interface IListarMovimentacoesUseCase
    {
        public Task<ResponseMovimentacoes> Execute(RequestListarMovimentacoes request);
    }
}
