using Domain.Entities;

namespace Domain.Repositories
{
    public interface IEstoqueRepository
    {
        public Task<Estoque?> GetByVarianteId(int varianteId);

        public Task Adicionar(Estoque estoque);

        public void Update(Estoque estoque);
    }
}
