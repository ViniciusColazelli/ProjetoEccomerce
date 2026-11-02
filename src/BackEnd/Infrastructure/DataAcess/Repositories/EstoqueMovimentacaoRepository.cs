using Domain.Entities;
using Domain.Filtros;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class EstoqueMovimentacaoRepository : IEstoqueMovimentacaoRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public EstoqueMovimentacaoRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task<IList<EstoqueMovimentacoes>> Listar(FiltroListagemMovimentacoes filtro)
        {
            return await Consultar(filtro)
                .Include(movimentacao => movimentacao.Variante).ThenInclude(variante => variante.Produto)
                .Include(movimentacao => movimentacao.Variante).ThenInclude(variante => variante.TamanhoOpcao)
                .Include(movimentacao => movimentacao.Variante).ThenInclude(variante => variante.CorOpcao)
                .OrderByDescending(movimentacao => movimentacao.CriadoEm)
                .ThenByDescending(movimentacao => movimentacao.Id)
                .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
                .Take(filtro.TamanhoPagina)
                .ToListAsync();
        }

        public async Task<int> Contar(FiltroListagemMovimentacoes filtro)
        {
            return await Consultar(filtro).CountAsync();
        }

        public async Task Adicionar(EstoqueMovimentacoes movimentacao)
        {
            await _dbcontext.EstoqueMovimentacoes.AddAsync(movimentacao);
        }

        // listagem e contagem usam os mesmos filtros para o total bater
        private IQueryable<EstoqueMovimentacoes> Consultar(FiltroListagemMovimentacoes filtro)
        {
            var consulta = _dbcontext.EstoqueMovimentacoes.AsNoTracking();

            if (filtro.VarianteId.HasValue)
                consulta = consulta.Where(movimentacao => movimentacao.VarianteId == filtro.VarianteId.Value);

            if (filtro.ProdutoId.HasValue)
                consulta = consulta.Where(movimentacao => movimentacao.Variante.ProdutoId == filtro.ProdutoId.Value);

            if (filtro.PedidoId.HasValue)
                consulta = consulta.Where(movimentacao => movimentacao.PedidoId == filtro.PedidoId.Value);

            if (filtro.Tipo.HasValue)
                consulta = consulta.Where(movimentacao => movimentacao.Tipo == filtro.Tipo.Value);

            // criado_em é timestamptz: o Npgsql só aceita DateTime em UTC, então o dia é contado em UTC
            if (filtro.De.HasValue)
            {
                var inicio = filtro.De.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                consulta = consulta.Where(movimentacao => movimentacao.CriadoEm >= inicio);
            }

            // até o fim do dia informado
            if (filtro.Ate.HasValue)
            {
                var fim = filtro.Ate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                consulta = consulta.Where(movimentacao => movimentacao.CriadoEm < fim);
            }

            return consulta;
        }
    }
}
