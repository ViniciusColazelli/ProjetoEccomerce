using Application.Request;
using Application.Response;

namespace Application.UseCases.ProdutoImagem.Ordenar
{
    public interface IOrdenarImagensUseCase
    {
        public Task<IList<ResponseProdutoImagem>> Execute(int produtoId, RequestOrdenarImagens request);
    }
}
