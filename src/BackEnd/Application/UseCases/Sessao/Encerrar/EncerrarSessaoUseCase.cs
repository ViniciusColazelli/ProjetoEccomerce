using Domain.Repositories;
using Domain.Security.Tokens;

namespace Application.UseCases.Sessao.Encerrar
{
    // Logout: revoga a sessão no banco, e não só apaga o cookie. Uma cópia do cookie para de funcionar também.
    // Sem cookie, com token desconhecido ou já revogado, não há o que fazer: o logout nunca falha
    public class EncerrarSessaoUseCase : IEncerrarSessaoUseCase
    {
        private readonly ISessaoRepository _sessaoRepository;
        private readonly IGeradorRefreshToken _geradorRefreshToken;
        private readonly ISalvarDBRepository _salvarDB;

        public EncerrarSessaoUseCase(ISessaoRepository sessaoRepository, IGeradorRefreshToken geradorRefreshToken, ISalvarDBRepository salvarDB)
        {
            _sessaoRepository = sessaoRepository;
            _geradorRefreshToken = geradorRefreshToken;
            _salvarDB = salvarDB;
        }

        public async Task Execute(string? refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            var sessao = await _sessaoRepository.GetByTokenHash(_geradorRefreshToken.CalcularHash(refreshToken));

            if (sessao is null || sessao.RevogadoEm is not null)
                return;

            sessao.RevogadoEm = DateTime.UtcNow;

            await _salvarDB.Salvar();
        }
    }
}
