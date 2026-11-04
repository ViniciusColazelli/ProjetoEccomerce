using Domain.Entities;
using Domain.Enums;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.Services.MovimentarEstoque
{
    // Altera o saldo e registra a movimentação sem salvar: o use case chama o Salvar(), e os dois vão no mesmo SaveChanges
    public class MovimentarEstoque : IMovimentarEstoque
    {
        private readonly IEstoqueRepository _estoqueRepository;
        private readonly IEstoqueMovimentacaoRepository _movimentacaoRepository;
        private readonly IProdutoVarianteRepository _varianteRepository;

        public MovimentarEstoque(IEstoqueRepository estoqueRepository, IEstoqueMovimentacaoRepository movimentacaoRepository, IProdutoVarianteRepository varianteRepository)
        {
            _estoqueRepository = estoqueRepository;
            _movimentacaoRepository = movimentacaoRepository;
            _varianteRepository = varianteRepository;
        }

        public async Task<Estoque> Entrada(int varianteId, int quantidade, string motivo, int? pedidoId = null)
        {
            return await Movimentar(varianteId, TipoMovimentacao.Entrada, quantidade, motivo, pedidoId);
        }

        public async Task<Estoque> Saida(int varianteId, int quantidade, string motivo, int? pedidoId = null)
        {
            return await Movimentar(varianteId, TipoMovimentacao.Saida, quantidade, motivo, pedidoId);
        }

        private async Task<Estoque> Movimentar(int varianteId, TipoMovimentacao tipo, int quantidade, string motivo, int? pedidoId)
        {
            var estoque = await _estoqueRepository.GetByVarianteId(varianteId);
            var estoqueNovo = estoque is null;

            // variantes antigas, criadas pelo script SQL, podem não ter a linha de estoque
            if (estoque is null)
            {
                var varianteExiste = await _varianteRepository.Existe(varianteId);

                if (varianteExiste.IsFalse())
                    throw new NaoEncontradoException(ResourceMensagensDeErro.VARIANTE_NAO_ENCONTRADA);

                estoque = new Estoque { VarianteId = varianteId, Quantidade = 0 };
            }

            if (tipo == TipoMovimentacao.Saida)
            {
                if (estoque.Quantidade < quantidade)
                    throw new ConflitoException(ResourceMensagensDeErro.ESTOQUE_INSUFICIENTE);

                estoque.Quantidade -= quantidade;
            }
            else
            {
                estoque.Quantidade += quantidade;
            }

            if (estoqueNovo)
                await _estoqueRepository.Adicionar(estoque);
            else
                _estoqueRepository.Update(estoque);

            await _movimentacaoRepository.Adicionar(new EstoqueMovimentacoes
            {
                VarianteId = varianteId,
                Tipo = tipo,
                Quantidade = quantidade,
                Motivo = motivo,
                PedidoId = pedidoId
            });

            return estoque;
        }
    }
}
