namespace Application.Response
{
    // Vai para os cookies HttpOnly (CookieAutenticacaoExtension), nunca para o corpo da resposta
    public sealed record TokensSessao(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiraEm);
}
