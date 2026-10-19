using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Application.UseCases.Produto.Listar;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Produto.ListarAdmin
{
    public class ListarProdutosAdminUseCase : IListarProdutosAdminUseCase
    {
        private readonly IProdutoRepository _produtoRepository;

        public ListarProdutosAdminUseCase(IProdutoRepository produtoRepository)
        {
            _produtoRepository = produtoRepository;
        }

        public async Task<ResponseProdutosAdmin> Execute(RequestListarProdutosAdmin request)
        {
            Validate(request);

            var filtro = MapearRequest.RequestParaFiltroAdmin(request);

            var produtos = await _produtoRepository.ListarTodos(filtro);
            var total = await _produtoRepository.ContarTodos(filtro);

            return new ResponseProdutosAdmin
            {
                Produtos = produtos.Select(produto => new ResponseProdutoResumoAdmin
                {
                    Id = produto.Id,
                    Nome = produto.Nome,
                    Descricao = produto.Descricao,
                    PrecoBase = produto.PrecoBase,
                    Ativo = produto.Ativo
                }).ToList(),
                Pagina = request.Pagina,
                TamanhoPagina = request.TamanhoPagina,
                Total = total
            };
        }

        private static void Validate(RequestListarProdutosAdmin request)
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
