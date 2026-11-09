using Domain.Entities;

namespace Domain.Repositories
{
    public interface ISessaoRepository
    {
        public Task Adicionar(Sessoes sessao);

        // com o cliente e com rastreamento: o refresh revoga a sessão e emite o JWT com os dados atuais do cliente
        public Task<Sessoes?> GetByTokenHash(string tokenHash);

        // ativas agora, das mais antigas para as mais novas, com rastreamento (para revogar)
        public Task<IList<Sessoes>> ListarAtivas(int clienteId, DateTime agora);

        // Apaga as vencidas do cliente. As revogadas ficam até vencer: um token já trocado
        // que reaparece ainda é reconhecido como reuso
        public Task RemoverVencidas(int clienteId, DateTime agora);
    }
}
