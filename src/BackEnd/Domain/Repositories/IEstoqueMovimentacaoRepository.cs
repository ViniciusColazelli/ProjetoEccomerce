using Domain.Entities;
using Domain.Filtros;

namespace Domain.Repositories
{
    public interface IEstoqueMovimentacaoRepository
    {
        public Task<IList<EstoqueMovimentacoes>> Listar(FiltroListagemMovimentacoes filtro);

        public Task<int> Contar(FiltroListagemMovimentacoes filtro);

        public Task Adicionar(EstoqueMovimentacoes movimentacao);
    }
}
