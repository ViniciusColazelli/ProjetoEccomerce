using Infrastructure.Security.Tokens;

namespace API.Extensions
{
    public static class CookieAutenticacaoExtension
    {
        public static void EscreverTokenNoCookie(this HttpResponse response, string token, IConfiguration configuration)
        {
            var minutos = configuration.GetValue<int>("Jwt:ExpiracaoEmMinutos");

            response.Cookies.Append(
                CookieAutenticacao.Nome,
                token,
                Opcoes(response.HttpContext, DateTimeOffset.UtcNow.AddMinutes(minutos)));
        }

        public static void RemoverTokenDoCookie(this HttpResponse response)
        {
            response.Cookies.Delete(CookieAutenticacao.Nome, Opcoes(response.HttpContext, null));
        }

        private static CookieOptions Opcoes(HttpContext context, DateTimeOffset? expira) => new()
        {
            HttpOnly = true,                      // JavaScript não lê
            Secure = context.Request.IsHttps,     // só HTTPS em produção
            SameSite = SameSiteMode.Lax,          // bloqueia envio em requisições cross-site
            Path = "/",
            Expires = expira
        };
    }
}