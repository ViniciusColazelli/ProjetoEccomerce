using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Domain.Entities;
using Domain.Security.Tokens;

namespace Infrastructure.Security.Tokens
{
    public class GeradorRefreshToken : IGeradorRefreshToken
    {
        // 256 bits aleatórios: impossível de adivinhar, então o hash não precisa de sal nem de BCrypt
        private const int TAMANHO_EM_BYTES = 32;

        private readonly SessaoConfig _config;

        public GeradorRefreshToken(SessaoConfig config)
        {
            _config = config;
        }

        public RefreshTokenGerado Criar(int clienteId)
        {
            var agora = DateTime.UtcNow;

            return Gerar(clienteId, agora, agora.AddDays(_config.DuracaoMaximaEmDias));
        }

        public RefreshTokenGerado Renovar(Sessoes anterior) =>
            Gerar(anterior.ClienteId, DateTime.UtcNow, anterior.ExpiraMaximoEm);

        public string CalcularHash(string token) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private RefreshTokenGerado Gerar(int clienteId, DateTime agora, DateTime expiraMaximoEm)
        {
            // base64url: vai no cookie sem precisar de escape
            var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TAMANHO_EM_BYTES));

            var expiraEm = agora.AddDays(_config.ExpiracaoEmDias);

            var sessao = new Sessoes
            {
                ClienteId = clienteId,
                TokenHash = CalcularHash(token),
                ExpiraEm = expiraEm < expiraMaximoEm ? expiraEm : expiraMaximoEm,
                ExpiraMaximoEm = expiraMaximoEm
            };

            return new RefreshTokenGerado(token, sessao);
        }
    }
}
