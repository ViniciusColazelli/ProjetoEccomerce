using Domain.Entities;

namespace Domain.Repositories
{
    public interface IEstoqueMovimentacaoRepository
    {
        public Task Adicionar(EstoqueMovimentacoes movimentacao);
    }
}
