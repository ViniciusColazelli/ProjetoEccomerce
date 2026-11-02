using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions.ExceptionBase;

namespace Application.UseCases.EstoqueVariante.Historico
{
    public class ListarMovimentacoesUseCase : IListarMovimentacoesUseCase
    {
        private readonly IEstoqueMovimentacaoRepository _movimentacaoRepository;

        public ListarMovimentacoesUseCase(IEstoqueMovimentacaoRepository movimentacaoRepository)
        {
            _movimentacaoRepository = movimentacaoRepository;
        }

        public async Task<ResponseMovimentacoes> Execute(RequestListarMovimentacoes request)
        {
            Validate(request);

            var filtro = MapearRequest.RequestParaFiltro(request);

            var movimentacoes = await _movimentacaoRepository.Listar(filtro);
            var total = await _movimentacaoRepository.Contar(filtro);

            return new ResponseMovimentacoes
            {
                Movimentacoes = movimentacoes.Select(movimentacao => new ResponseMovimentacao
                {
                    Id = movimentacao.Id,
                    VarianteId = movimentacao.VarianteId,
                    ProdutoId = movimentacao.Variante.ProdutoId,
                    Produto = movimentacao.Variante.Produto.Nome,
                    Tamanho = movimentacao.Variante.TamanhoOpcao.Valor,
                    Cor = movimentacao.Variante.CorOpcao?.Valor,
                    Tipo = movimentacao.Tipo,
                    Quantidade = movimentacao.Quantidade,
                    Motivo = movimentacao.Motivo,
                    PedidoId = movimentacao.PedidoId,
                    CriadoEm = movimentacao.CriadoEm
                }).ToList(),
                Pagina = request.Pagina,
                TamanhoPagina = request.TamanhoPagina,
                Total = total
            };
        }

        private static void Validate(RequestListarMovimentacoes request)
        {
            var validator = new ListarMovimentacoesValidator();

            var result = validator.Validate(request);

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}
