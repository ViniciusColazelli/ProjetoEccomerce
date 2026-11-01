using Application.Request;
using Application.Response;
using Application.Services.MovimentarEstoque;
using Application.UseCases.EstoqueVariante.Entrada;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions.ExceptionBase;

namespace Application.UseCases.EstoqueVariante.Saida
{
    public class SaidaEstoqueUseCase : ISaidaEstoqueUseCase
    {
        private readonly IMovimentarEstoque _movimentarEstoque;
        private readonly IProdutoVarianteRepository _varianteRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public SaidaEstoqueUseCase(IMovimentarEstoque movimentarEstoque, IProdutoVarianteRepository varianteRepository, ISalvarDBRepository salvarDB)
        {
            _movimentarEstoque = movimentarEstoque;
            _varianteRepository = varianteRepository;
            _salvarDB = salvarDB;
        }

        public async Task<ResponseEstoqueVariante> Execute(int varianteId, RequestMovimentarEstoque request)
        {
            Validate(request);

            await _movimentarEstoque.Saida(varianteId, request.Quantidade, request.Motivo.Trim());

            await _salvarDB.Salvar();

            var variante = (await _varianteRepository.ObterComEstoque(varianteId))!;

            return new ResponseEstoqueVariante
            {
                VarianteId = variante.Id,
                ProdutoId = variante.ProdutoId,
                Produto = variante.Produto.Nome,
                Tamanho = variante.TamanhoOpcao.Valor,
                Cor = variante.CorOpcao?.Valor,
                Ativo = variante.Ativo,
                Quantidade = variante.Estoque?.Quantidade ?? 0
            };
        }

        private static void Validate(RequestMovimentarEstoque request)
        {
            var validator = new MovimentarEstoqueValidator();

            var result = validator.Validate(request);

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}
