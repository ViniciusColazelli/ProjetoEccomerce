using Application.Response;
using Domain.Entities;
using Domain.Repositories;
using Domain.Security.Tokens;
using Exceptions.ExceptionBase;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Sessao.Renovar
{
    // Troca o refresh token por um JWT novo e um refresh token novo (rotação: cada refresh token vale uma vez)
    public class RenovarSessaoUseCase : IRenovarSessaoUseCase
    {
        // Duas abas renovando juntas mandam o mesmo token: a segunda chega logo depois da troca.
        // Dentro desse tempo, o token já trocado só é recusado; depois dele, é tratado como roubo
        public static readonly TimeSpan ToleranciaReuso = TimeSpan.FromSeconds(30);

        private readonly ISessaoRepository _sessaoRepository;
        private readonly IGeradorRefreshToken _geradorRefreshToken;
        private readonly IGeradorTokenJwt _geradorTokenJwt;
        private readonly ISalvarDBRepository _salvarDB;
        private readonly ILogger<RenovarSessaoUseCase> _logger;

        public RenovarSessaoUseCase(ISessaoRepository sessaoRepository, IGeradorRefreshToken geradorRefreshToken, IGeradorTokenJwt geradorTokenJwt,
            ISalvarDBRepository salvarDB, ILogger<RenovarSessaoUseCase> logger)
        {
            _sessaoRepository = sessaoRepository;
            _geradorRefreshToken = geradorRefreshToken;
            _geradorTokenJwt = geradorTokenJwt;
            _salvarDB = salvarDB;
            _logger = logger;
        }

        public async Task<TokensSessao> Execute(string? refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ClienteNaoLogadoException();

            var agora = DateTime.UtcNow;
            var sessao = await _sessaoRepository.GetByTokenHash(_geradorRefreshToken.CalcularHash(refreshToken));

            if (sessao is null)
                throw new ClienteNaoLogadoException();

            if (sessao.RevogadoEm is not null)
            {
                if (TokenRoubado(sessao, agora))
                    await RevogarTodas(sessao.ClienteId, agora);

                throw new ClienteNaoLogadoException();
            }

            if (sessao.Ativa(agora) == false)
                throw new ClienteNaoLogadoException();   // vencida (sem uso ou no limite máximo)

            var nova = _geradorRefreshToken.Renovar(sessao);
            sessao.RevogadoEm = agora;
            sessao.SubstituidaPor = nova.Sessao;
            await _sessaoRepository.Adicionar(nova.Sessao);

            try
            {
                await _salvarDB.Salvar();
            }
            // outra requisição renovou esta mesma sessão ao mesmo tempo (xmin): ela ganhou, esta só é recusada
            catch (ConflitoException)
            {
                throw new ClienteNaoLogadoException();
            }

            // o cliente vem do banco: nome, e-mail e perfil atualizados entram no JWT novo
            var accessToken = _geradorTokenJwt.GerarToken(sessao.Cliente);

            return new TokensSessao(accessToken, nova.Token, nova.Sessao.ExpiraEm);
        }

        // Já trocado por outro e reapresentado depois da tolerância: alguém tem uma cópia do cookie.
        // Revogada sem substituta (logout, troca de senha) não é roubo: só é recusada
        private static bool TokenRoubado(Sessoes sessao, DateTime agora) =>
            sessao.SubstituidaPorId is not null && agora - sessao.RevogadoEm > ToleranciaReuso;

        // derruba o ladrão e o dono: os dois precisam logar de novo, e só o dono sabe a senha
        private async Task RevogarTodas(int clienteId, DateTime agora)
        {
            var ativas = await _sessaoRepository.ListarAtivas(clienteId, agora);

            foreach (var ativa in ativas)
                ativa.RevogadoEm = agora;

            try
            {
                await _salvarDB.Salvar();
            }
            catch (ConflitoException)
            {
                // uma das sessões mudou no meio do caminho (renovação simultânea): a recusa continua valendo
            }

            _logger.LogWarning("Refresh token já trocado foi reapresentado: {Quantidade} sessões do cliente {ClienteId} revogadas",
                ativas.Count, clienteId);
        }
    }
}
