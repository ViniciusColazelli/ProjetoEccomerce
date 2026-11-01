using Domain.Entities;
using Domain.Repositories;

namespace Infrastructure.DataAcess.Repositories
{
    public class EstoqueMovimentacaoRepository : IEstoqueMovimentacaoRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public EstoqueMovimentacaoRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task Adicionar(EstoqueMovimentacoes movimentacao)
        {
            await _dbcontext.EstoqueMovimentacoes.AddAsync(movimentacao);
        }
    }
}
