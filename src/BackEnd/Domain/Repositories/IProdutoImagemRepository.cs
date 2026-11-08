using Domain.Entities;

namespace Domain.Repositories
{
    public interface IProdutoImagemRepository
    {
        // na ordem de exibição (a primeira é a capa), com rastreamento: a reordenação altera e salva
        public Task<IList<ProdutoImagens>> ListarPorProduto(int produtoId);

        public Task<ProdutoImagens?> GetById(int produtoId, int imagemId);

        public Task<int> Contar(int produtoId);

        public Task Adicionar(ProdutoImagens imagem);

        public void Remover(ProdutoImagens imagem);
    }
}
