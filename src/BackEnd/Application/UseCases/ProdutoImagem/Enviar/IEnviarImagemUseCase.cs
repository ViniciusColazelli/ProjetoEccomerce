using Application.Request;
using Application.Response;

namespace Application.UseCases.ProdutoImagem.Enviar
{
    public interface IEnviarImagemUseCase
    {
        public Task<ResponseProdutoImagem> Execute(int produtoId, RequestEnviarImagem request);
    }
}
