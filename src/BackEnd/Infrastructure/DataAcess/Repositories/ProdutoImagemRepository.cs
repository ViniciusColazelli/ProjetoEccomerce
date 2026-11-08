using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class ProdutoImagemRepository : IProdutoImagemRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public ProdutoImagemRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task<IList<ProdutoImagens>> ListarPorProduto(int produtoId)
        {
            return await _dbcontext.ProdutoImagens
                .Where(imagem => imagem.ProdutoId == produtoId)
                .OrderBy(imagem => imagem.Ordem)
                .ThenBy(imagem => imagem.Id)
                .ToListAsync();
        }

        public async Task<ProdutoImagens?> GetById(int produtoId, int imagemId)
        {
            return await _dbcontext.ProdutoImagens
                .FirstOrDefaultAsync(imagem => imagem.Id == imagemId && imagem.ProdutoId == produtoId);
        }

        public async Task<int> Contar(int produtoId)
        {
            return await _dbcontext.ProdutoImagens.CountAsync(imagem => imagem.ProdutoId == produtoId);
        }

        public async Task Adicionar(ProdutoImagens imagem)
        {
            await _dbcontext.ProdutoImagens.AddAsync(imagem);
        }

        public void Remover(ProdutoImagens imagem)
        {
            _dbcontext.ProdutoImagens.Remove(imagem);
        }
    }
}
