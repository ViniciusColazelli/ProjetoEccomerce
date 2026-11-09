namespace Infrastructure.Security.Tokens
{
    public static class CookieAutenticacao
    {
        public const string Nome = "access_token";

        // refresh token: só serve para POST /api/Login/refresh e para o logout
        public const string NomeRefresh = "refresh_token";
    }
}