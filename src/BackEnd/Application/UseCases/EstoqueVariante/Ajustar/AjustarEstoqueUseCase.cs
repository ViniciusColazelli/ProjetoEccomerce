using Application.Request;
using Application.Response;
using Application.Services.MovimentarEstoque;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.EstoqueVariante.Ajustar
{
    // O ajuste de inventário vira uma Entrada ou uma Saida com a diferença entre o saldo contado e o atual
    public class AjustarEstoqueUseCase : IAjustarEstoqueUseCase
    {
        private readonly IMovimentarEstoque _movimentarEstoque;
        private readonly IEstoqueRepository _estoqueRepository;
        private readonly IProdutoVarianteRepository _varianteRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public AjustarEstoqueUseCase(IMovimentarEstoque movimentarEstoque, IEstoqueRepository estoqueRepository, IProdutoVarianteRepository varianteRepository, ISalvarDBRepository salvarDB)
        {
            _movimentarEstoque = movimentarEstoque;
            _estoqueRepository = estoqueRepository;
            _varianteRepository = varianteRepository;
            _salvarDB = salvarDB;
        }

        public async Task<ResponseEstoqueVariante> Execute(int varianteId, RequestAjustarEstoque request)
        {
            var estoque = await _estoqueRepository.GetByVarianteId(varianteId);

            if (estoque is null)
            {
                var varianteExiste = await _varianteRepository.Existe(varianteId);

                if (varianteExiste.IsFalse())
                    throw new NaoEncontradoException(ResourceMensagensDeErro.VARIANTE_NAO_ENCONTRADA);
            }

            var saldoAtual = estoque is null ? 0 : estoque.Quantidade;

            Validate(request, saldoAtual);

            var diferenca = request.NovaQuantidade - saldoAtual;
            var motivo = request.Motivo.Trim();

            if (diferenca > 0)
                await _movimentarEstoque.Entrada(varianteId, diferenca, motivo);
            else
                await _movimentarEstoque.Saida(varianteId, -diferenca, motivo);

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

        private static void Validate(RequestAjustarEstoque request, int saldoAtual)
        {
            var validator = new AjustarEstoqueValidator();

            var result = validator.Validate(request);

            if (request.NovaQuantidade == saldoAtual)
                result.Errors.Add(new FluentValidation.Results.ValidationFailure(string.Empty, ResourceMensagensDeErro.AJUSTE_SEM_DIFERENCA));

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}
