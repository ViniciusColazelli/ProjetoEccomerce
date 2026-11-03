using Domain.Entities;

namespace Application.Services.MovimentarEstoque
{
    public interface IMovimentarEstoque
    {
        public Task<Estoque> Entrada(int varianteId, int quantidade, string motivo);

        public Task<Estoque> Saida(int varianteId, int quantidade, string motivo);
    }
}
