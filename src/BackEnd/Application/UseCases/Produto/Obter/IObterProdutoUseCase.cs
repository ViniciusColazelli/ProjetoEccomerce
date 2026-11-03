using Application.Response;

namespace Application.UseCases.Produto.Obter
{
    public interface IObterProdutoUseCase
    {
        public Task<ResponseProdutoDetalhe> Execute(int id);
    }
}
