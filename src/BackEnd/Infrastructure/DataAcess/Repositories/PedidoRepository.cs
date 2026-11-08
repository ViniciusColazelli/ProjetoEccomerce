using Domain.Entities;
using Domain.Filtros;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public PedidoRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task Adicionar(Pedidos pedido)
        {
            await _dbcontext.Pedidos.AddAsync(pedido);
        }

        public async Task<Pedidos?> ObterComDetalhes(int pedidoId)
        {
            return await IncluirDetalhes(_dbcontext.Pedidos.AsNoTracking())
                .FirstOrDefaultAsync(pedido => pedido.Id == pedidoId);
        }

        public async Task<Pedidos?> GetById(int pedidoId)
        {
            return await _dbcontext.Pedidos
                .Include(pedido => pedido.Itens)
                .FirstOrDefaultAsync(pedido => pedido.Id == pedidoId);
        }

        public async Task<IList<Pedidos>> Listar(FiltroListagemPedidos filtro)
        {
            return await Consultar(filtro)
                .Include(pedido => pedido.Cliente)
                .Include(pedido => pedido.Itens)
                .OrderByDescending(pedido => pedido.CriadoEm)
                .ThenByDescending(pedido => pedido.Id)
                .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
                .Take(filtro.TamanhoPagina)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<int> Contar(FiltroListagemPedidos filtro)
        {
            return await Consultar(filtro).CountAsync();
        }

        // listagem e contagem usam os mesmos filtros para o total bater
        private IQueryable<Pedidos> Consultar(FiltroListagemPedidos filtro)
        {
            var consulta = _dbcontext.Pedidos.AsNoTracking();

            if (filtro.ClienteId.HasValue)
                consulta = consulta.Where(pedido => pedido.ClienteId == filtro.ClienteId.Value);

            if (filtro.Status.HasValue)
                consulta = consulta.Where(pedido => pedido.Status == filtro.Status.Value);

            if (string.IsNullOrWhiteSpace(filtro.BuscaCliente) == false)
            {
                // Ignora maiúsculas e acentos, como a busca de produtos: "joao" encontra "João" (unaccent, migration 15)
                var busca = filtro.BuscaCliente.Trim();
                consulta = consulta.Where(pedido =>
                    EF.Functions.Unaccent(pedido.Cliente.Nome).ToLower().Contains(EF.Functions.Unaccent(busca).ToLower())
                    || EF.Functions.Unaccent(pedido.Cliente.Email).ToLower().Contains(EF.Functions.Unaccent(busca).ToLower()));
            }

            // o dia é contado em UTC, como no histórico de movimentações
            if (filtro.De.HasValue)
            {
                var inicio = filtro.De.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                consulta = consulta.Where(pedido => pedido.CriadoEm >= inicio);
            }

            if (filtro.Ate.HasValue)
            {
                var fim = filtro.Ate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                consulta = consulta.Where(pedido => pedido.CriadoEm < fim);
            }

            return consulta;
        }

        // AsSplitQuery: itens × customizações num JOIN só repetiriam as linhas do pedido
        private static IQueryable<Pedidos> IncluirDetalhes(IQueryable<Pedidos> consulta)
        {
            return consulta
                .Include(pedido => pedido.Cliente)
                .Include(pedido => pedido.Itens).ThenInclude(item => item.Produto)
                .Include(pedido => pedido.Itens).ThenInclude(item => item.Variante).ThenInclude(variante => variante!.TamanhoOpcao)
                .Include(pedido => pedido.Itens).ThenInclude(item => item.Variante).ThenInclude(variante => variante!.CorOpcao)
                .Include(pedido => pedido.Itens).ThenInclude(item => item.Customizacoes)
                .AsSplitQuery();
        }
    }
}
