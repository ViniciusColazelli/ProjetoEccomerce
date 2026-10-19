using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class ProdutoOpcaoRepository : IProdutoOpcaoRepository
    {
        private readonly EccomerceDbContext _dbcontext;

        public ProdutoOpcaoRepository(EccomerceDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task<ProdutoOpcoes?> GetById(int produtoId, int opcaoId)
        {
            return await _dbcontext.ProdutoOpcoes.FirstOrDefaultAsync(opcao => opcao.Id == opcaoId && opcao.ProdutoId == produtoId);
        }

        public async Task<IList<ProdutoOpcoes>> ListarAtivas(int produtoId)
        {
            return await _dbcontext.ProdutoOpcoes
                .AsNoTracking()
                .Where(opcao => opcao.ProdutoId == produtoId && opcao.Ativo)
                .OrderBy(opcao => opcao.Id)
                .ToListAsync();
        }

        public async Task<bool> ExisteOpcao(int produtoId, TipoOpcao tipo, string valor, int? ignorarOpcaoId = null)
        {
            var valorNormalizado = valor.Trim().ToLower();

            return await _dbcontext.ProdutoOpcoes.AnyAsync(opcao =>
                opcao.ProdutoId == produtoId &&
                opcao.Tipo == tipo &&
                opcao.Valor.ToLower() == valorNormalizado &&
                opcao.Id != ignorarOpcaoId);
        }

        public async Task<bool> EstaEmUso(int opcaoId)
        {
            var usadaEmVariante = await _dbcontext.ProdutoVariantes
                .AnyAsync(variante => variante.TamanhoOpcaoId == opcaoId || variante.CorOpcaoId == opcaoId);

            if (usadaEmVariante)
                return true;

            // pedido_item_customizacoes ainda não tem entidade no EF
            return await _dbcontext.Database
                .SqlQuery<bool>($"SELECT EXISTS (SELECT 1 FROM pedido_item_customizacoes WHERE opcao_id = {opcaoId}) AS \"Value\"")
                .SingleAsync();
        }

        public async Task Adicionar(ProdutoOpcoes opcao)
        {
            await _dbcontext.ProdutoOpcoes.AddAsync(opcao);
        }

        public void Update(ProdutoOpcoes opcao)
        {
            _dbcontext.ProdutoOpcoes.Update(opcao);
        }

        public void Remover(ProdutoOpcoes opcao)
        {
            _dbcontext.ProdutoOpcoes.Remove(opcao);
        }
    }
}
