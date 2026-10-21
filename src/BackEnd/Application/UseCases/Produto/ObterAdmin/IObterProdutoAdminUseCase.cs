using Application.Response;

namespace Application.UseCases.Produto.ObterAdmin
{
    public interface IObterProdutoAdminUseCase
    {
        public Task<ResponseProdutoAdmin> Execute(int id);
    }
}
