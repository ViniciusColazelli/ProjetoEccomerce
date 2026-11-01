using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions.ExceptionBase;

namespace Application.UseCases.EstoqueVariante.Listar
{
    public class ListarEstoqueUseCase : IListarEstoqueUseCase
    {
        private readonly IProdutoVarianteRepository _varianteRepository;

        public ListarEstoqueUseCase(IProdutoVarianteRepository varianteRepository)
        {
            _varianteRepository = varianteRepository;
        }

        public async Task<ResponseEstoque> Execute(RequestListarEstoque request)
        {
            Validate(request);

            var filtro = MapearRequest.RequestParaFiltro(request);

            var variantes = await _varianteRepository.ListarComEstoque(filtro);
            var total = await _varianteRepository.ContarComEstoque(filtro);

            return new ResponseEstoque
            {
                Variantes = variantes.Select(variante => new ResponseEstoqueVariante
                {
                    VarianteId = variante.Id,
                    ProdutoId = variante.ProdutoId,
                    Produto = variante.Produto.Nome,
                    Tamanho = variante.TamanhoOpcao.Valor,
                    Cor = variante.CorOpcao?.Valor,
                    Ativo = variante.Ativo,
                    Quantidade = variante.Estoque?.Quantidade ?? 0
                }).ToList(),
                Pagina = request.Pagina,
                TamanhoPagina = request.TamanhoPagina,
                Total = total
            };
        }

        private static void Validate(RequestListarEstoque request)
        {
            var validator = new ListarEstoqueValidator();

            var result = validator.Validate(request);

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}
