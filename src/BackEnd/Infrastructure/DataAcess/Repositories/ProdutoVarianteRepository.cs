using Domain.Entities;
using Domain.Filtros;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class ProdutoVarianteRepository : IProdutoVarianteRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public ProdutoVarianteRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task<ProdutoVariantes?> GetById(int produtoId, int varianteId)
        {
            return await _dbcontext.ProdutoVariantes.FirstOrDefaultAsync(variante => variante.Id == varianteId && variante.ProdutoId == produtoId);
        }

        public async Task<bool> Existe(int varianteId)
        {
            return await _dbcontext.ProdutoVariantes.AnyAsync(variante => variante.Id == varianteId);
        }

        public async Task<IList<ProdutoVariantes>> ListarPorProduto(int produtoId)
        {
            return await _dbcontext.ProdutoVariantes
                .AsNoTracking()
                .Where(variante => variante.ProdutoId == produtoId)
                .ToListAsync();
        }

        public async Task<IList<ProdutoVariantes>> ListarComEstoque(FiltroListagemEstoque filtro)
        {
            return await ConsultarComEstoque(filtro)
                .OrderBy(variante => variante.Produto.Nome)
                .ThenBy(variante => variante.Id)
                .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
                .Take(filtro.TamanhoPagina)
                .ToListAsync();
        }

        public async Task<int> ContarComEstoque(FiltroListagemEstoque filtro)
        {
            return await ConsultarComEstoque(filtro).CountAsync();
        }

        public async Task<ProdutoVariantes?> ObterComEstoque(int varianteId)
        {
            return await IncluirDetalhes(_dbcontext.ProdutoVariantes.AsNoTracking())
                .FirstOrDefaultAsync(variante => variante.Id == varianteId);
        }

        public async Task Adicionar(IList<ProdutoVariantes> variantes)
        {
            await _dbcontext.ProdutoVariantes.AddRangeAsync(variantes);
        }

        public void Update(ProdutoVariantes variante)
        {
            _dbcontext.ProdutoVariantes.Update(variante);
        }

        // listagem e contagem usam os mesmos filtros para o total bater
        private IQueryable<ProdutoVariantes> ConsultarComEstoque(FiltroListagemEstoque filtro)
        {
            var consulta = IncluirDetalhes(_dbcontext.ProdutoVariantes.AsNoTracking());

            if (filtro.ProdutoId.HasValue)
                consulta = consulta.Where(variante => variante.ProdutoId == filtro.ProdutoId.Value);

            // variante sem linha de estoque conta como zerada
            if (filtro.SomenteZerados)
                consulta = consulta.Where(variante => variante.Estoque == null || variante.Estoque.Quantidade == 0);

            return consulta;
        }

        public async Task<IList<ProdutoVariantes>> ListarComEstoquePorIds(IList<int> varianteIds)
        {
            return await IncluirDetalhes(_dbcontext.ProdutoVariantes.AsNoTracking())
                .Where(variante => varianteIds.Contains(variante.Id))
                .ToListAsync();
        }

        private static IQueryable<ProdutoVariantes> IncluirDetalhes(IQueryable<ProdutoVariantes> consulta)
        {
            return consulta
                .Include(variante => variante.Produto)
                .Include(variante => variante.TamanhoOpcao)
                .Include(variante => variante.CorOpcao)
                .Include(variante => variante.Estoque);
        }
    }
}
