using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class ProdutoRepository : IProdutoRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public ProdutoRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task<IList<Produtos>> ListarAtivos(int pagina, int tamanhoPagina)
        {
            return await _dbcontext.Produtos
                .AsNoTracking()
                .Where(produto => produto.Ativo)
                .OrderBy(produto => produto.Nome)
                .ThenBy(produto => produto.Id)
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToListAsync();
        }

        public async Task<int> ContarAtivos()
        {
            return await _dbcontext.Produtos.CountAsync(produto => produto.Ativo);
        }
    }
}
