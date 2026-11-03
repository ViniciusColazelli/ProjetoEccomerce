using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories
{
    public interface IProdutoOpcaoRepository
    {
        public Task<ProdutoOpcoes?> GetById(int produtoId, int opcaoId);

        public Task<IList<ProdutoOpcoes>> ListarAtivas(int produtoId);

        public Task<bool> ExisteOpcao(int produtoId, TipoOpcao tipo, string valor, int? ignorarOpcaoId = null);

        public Task<bool> EstaEmUso(int opcaoId);

        public Task Adicionar(ProdutoOpcoes opcao);

        public void Update(ProdutoOpcoes opcao);

        public void Remover(ProdutoOpcoes opcao);
    }
}
