using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class SessaoRepository : ISessaoRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public SessaoRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task Adicionar(Sessoes sessao)
        {
            await _dbcontext.Sessoes.AddAsync(sessao);
        }

        public async Task<Sessoes?> GetByTokenHash(string tokenHash)
        {
            return await _dbcontext.Sessoes
                .Include(sessao => sessao.Cliente)
                .FirstOrDefaultAsync(sessao => sessao.TokenHash == tokenHash);
        }

        public async Task<IList<Sessoes>> ListarAtivas(int clienteId, DateTime agora)
        {
            return await _dbcontext.Sessoes
                .Where(sessao => sessao.ClienteId == clienteId && sessao.RevogadoEm == null && sessao.ExpiraEm > agora)
                .OrderBy(sessao => sessao.CriadoEm)
                .ThenBy(sessao => sessao.Id)
                .ToListAsync();
        }

        // poucas linhas por cliente: carrega e remove pelo EF, para valer o mesmo Salvar() dos outros repositórios
        public async Task RemoverVencidas(int clienteId, DateTime agora)
        {
            var vencidas = await _dbcontext.Sessoes
                .Where(sessao => sessao.ClienteId == clienteId && sessao.ExpiraEm <= agora)
                .ToListAsync();

            _dbcontext.Sessoes.RemoveRange(vencidas);
        }
    }
}
