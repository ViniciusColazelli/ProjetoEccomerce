namespace API.Extensions
{
    public static class HostingExtension
    {
        // No Render a porta vem da variável PORT; localmente a variável não existe e nada muda
        public static void UseEnvironmentPort(this IWebHostBuilder webHost)
        {
            var port = Environment.GetEnvironmentVariable("PORT");

            if (!string.IsNullOrEmpty(port))
            {
                webHost.UseUrls($"http://0.0.0.0:{port}");
            }
        }
    }
}