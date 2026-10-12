using Application.Request;
using Application.Response;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Produto.Listar
{
    public class ListarProdutosUseCase : IListarProdutosUseCase
    {
        private readonly IProdutoRepository _produtoRepository;

        public ListarProdutosUseCase(IProdutoRepository produtoRepository)
        {
            _produtoRepository = produtoRepository;
        }

        public async Task<ResponseProdutos> Execute(RequestListarProdutos request)
        {
            Validate(request);

            var produtos = await _produtoRepository.ListarAtivos(request.Pagina, request.TamanhoPagina);
            var total = await _produtoRepository.ContarAtivos();

            return new ResponseProdutos
            {
                Produtos = produtos.Select(produto => new ResponseProdutoResumo
                {
                    Id = produto.Id,
                    Nome = produto.Nome,
                    Descricao = produto.Descricao,
                    PrecoBase = produto.PrecoBase
                }).ToList(),
                Pagina = request.Pagina,
                TamanhoPagina = request.TamanhoPagina,
                Total = total
            };
        }

        private static void Validate(RequestListarProdutos request)
        {
            var validator = new ListarProdutosValidator();

            var result = validator.Validate(request);

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}
