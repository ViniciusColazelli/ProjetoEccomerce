using Domain.Entities;
using Domain.Enums;
using Domain.Extensions;
using Domain.Filtros;
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

        public async Task<IList<Produtos>> ListarAtivos(FiltroListagemProdutos filtro)
        {
            return await Paginar(Ordenar(ConsultarAtivos(filtro), filtro), filtro).ToListAsync();
        }

        public async Task<int> ContarAtivos(FiltroListagemProdutos filtro)
        {
            return await ConsultarAtivos(filtro).CountAsync();
        }

        public async Task<Produtos?> ObterAtivoComDetalhes(int id)
        {
            return await _dbcontext.Produtos
                .AsNoTracking()
                .Include(produto => produto.Opcoes.Where(opcao => opcao.Ativo))
                .Include(produto => produto.Variantes.Where(variante =>
                        variante.Ativo &&
                        variante.TamanhoOpcao.Ativo &&
                        (variante.CorOpcaoId == null || variante.CorOpcao!.Ativo)))
                    .ThenInclude(variante => variante.Estoque)
                .AsSplitQuery()
                .FirstOrDefaultAsync(produto => produto.Id == id && produto.Ativo);
        }

        public async Task<IList<Produtos>> ListarTodos(FiltroListagemProdutos filtro)
        {
            return await Paginar(Ordenar(ConsultarTodos(filtro), filtro), filtro).ToListAsync();
        }

        public async Task<int> ContarTodos(FiltroListagemProdutos filtro)
        {
            return await ConsultarTodos(filtro).CountAsync();
        }

        public async Task<Produtos?> ObterComDetalhes(int id)
        {
            return await _dbcontext.Produtos
                .AsNoTracking()
                .Include(produto => produto.Opcoes)
                .Include(produto => produto.Variantes).ThenInclude(variante => variante.TamanhoOpcao)
                .Include(produto => produto.Variantes).ThenInclude(variante => variante.CorOpcao)
                .Include(produto => produto.Variantes).ThenInclude(variante => variante.Estoque)
                .AsSplitQuery()
                .FirstOrDefaultAsync(produto => produto.Id == id);
        }

        public async Task<Produtos?> GetById(int id)
        {
            return await _dbcontext.Produtos.FirstOrDefaultAsync(produto => produto.Id == id);
        }

        public async Task<bool> Existe(int id)
        {
            return await _dbcontext.Produtos.AnyAsync(produto => produto.Id == id);
        }

        public async Task Adicionar(Produtos produto)
        {
            await _dbcontext.Produtos.AddAsync(produto);
        }

        public void Update(Produtos produto)
        {
            _dbcontext.Produtos.Update(produto);
        }

        private IQueryable<Produtos> ConsultarAtivos(FiltroListagemProdutos filtro)
        {
            var consulta = _dbcontext.Produtos
                .AsNoTracking()
                .Where(produto => produto.Ativo);

            return Filtrar(consulta, filtro);
        }

        private IQueryable<Produtos> ConsultarTodos(FiltroListagemProdutos filtro)
        {
            var consulta = _dbcontext.Produtos.AsNoTracking();

            if (filtro.Ativo.HasValue)
                consulta = consulta.Where(produto => produto.Ativo == filtro.Ativo.Value);

            return Filtrar(consulta, filtro);
        }

        // listagem e contagem usam os mesmos filtros para o total bater
        private static IQueryable<Produtos> Filtrar(IQueryable<Produtos> consulta, FiltroListagemProdutos filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro.Nome).IsFalse())
            {
                var nome = filtro.Nome!.Trim().ToLower();
                consulta = consulta.Where(produto => produto.Nome.ToLower().Contains(nome));
            }

            if (filtro.SomenteDisponiveis)
            {
                consulta = consulta.Where(produto => produto.Variantes.Any(variante =>
                    variante.Ativo &&
                    variante.TamanhoOpcao.Ativo &&
                    (variante.CorOpcaoId == null || variante.CorOpcao!.Ativo) &&
                    variante.Estoque != null &&
                    variante.Estoque.Quantidade > 0));
            }

            return consulta;
        }

        private static IQueryable<Produtos> Ordenar(IQueryable<Produtos> consulta, FiltroListagemProdutos filtro)
        {
            return (filtro.OrdenarPor, filtro.Decrescente) switch
            {
                (OrdenacaoProduto.Preco, false) => consulta.OrderBy(produto => produto.PrecoBase).ThenBy(produto => produto.Nome).ThenBy(produto => produto.Id),
                (OrdenacaoProduto.Preco, true) => consulta.OrderByDescending(produto => produto.PrecoBase).ThenBy(produto => produto.Nome).ThenBy(produto => produto.Id),
                (_, true) => consulta.OrderByDescending(produto => produto.Nome).ThenBy(produto => produto.Id),
                _ => consulta.OrderBy(produto => produto.Nome).ThenBy(produto => produto.Id)
            };
        }

        private static IQueryable<Produtos> Paginar(IQueryable<Produtos> consulta, FiltroListagemProdutos filtro)
        {
            return consulta
                .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
                .Take(filtro.TamanhoPagina);
        }
    }
}
