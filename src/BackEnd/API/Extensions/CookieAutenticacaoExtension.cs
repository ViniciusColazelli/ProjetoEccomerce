using Application.Response;
using Infrastructure.Security.Tokens;

namespace API.Extensions
{
    public static class CookieAutenticacaoExtension
    {
        // login e refresh: o JWT (minutos) e o refresh token (dias), os dois fora do alcance do JavaScript
        public static void EscreverCookiesDaSessao(this HttpResponse response, TokensSessao tokens, IConfiguration configuration)
        {
            var minutos = configuration.GetValue<int>("Jwt:ExpiracaoEmMinutos");

            response.Cookies.Append(
                CookieAutenticacao.Nome,
                tokens.AccessToken,
                OpcoesAccessToken(response.HttpContext, DateTimeOffset.UtcNow.AddMinutes(minutos)));

            response.Cookies.Append(
                CookieAutenticacao.NomeRefresh,
                tokens.RefreshToken,
                OpcoesRefreshToken(response.HttpContext, new DateTimeOffset(tokens.RefreshTokenExpiraEm, TimeSpan.Zero)));
        }

        public static void RemoverCookiesDaSessao(this HttpResponse response)
        {
            response.Cookies.Delete(CookieAutenticacao.Nome, OpcoesAccessToken(response.HttpContext, null));
            response.Cookies.Delete(CookieAutenticacao.NomeRefresh, OpcoesRefreshToken(response.HttpContext, null));
        }

        private static CookieOptions OpcoesAccessToken(HttpContext context, DateTimeOffset? expira) => new()
        {
            HttpOnly = true,                      // JavaScript não lê
            Secure = context.Request.IsHttps,     // só HTTPS em produção
            SameSite = SameSiteMode.Lax,          // bloqueia envio em requisições cross-site
            Path = "/",
            Expires = expira
        };

        // Strict: o refresh só acontece por chamada do próprio site, nunca ao chegar de um link externo
        private static CookieOptions OpcoesRefreshToken(HttpContext context, DateTimeOffset? expira) => new()
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expira
        };
    }
}
