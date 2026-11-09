using Domain.Entities;

namespace Domain.Security.Tokens
{
    public interface IGeradorRefreshToken
    {
        // Nova sessão (login): o limite máximo começa a contar agora
        RefreshTokenGerado Criar(int clienteId);

        // Rotação (refresh): nova sessão do mesmo cliente, que herda o limite máximo da anterior
        RefreshTokenGerado Renovar(Sessoes anterior);

        // Para achar a sessão pelo token que veio no cookie
        string CalcularHash(string token);
    }

    // Token: vai para o cookie e nunca é gravado. Sessao: ainda não salva, só com o hash do token
    public sealed record RefreshTokenGerado(string Token, Sessoes Sessao);
}
