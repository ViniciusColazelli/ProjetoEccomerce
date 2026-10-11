namespace API.Extensions
{
    public static class CorsExtension
    {
        public const string PoliticaFrontEnd = "FrontEnd";

        public static void AddCorsConfigured(this IServiceCollection services, IConfiguration configuration)
        {
            var origensPermitidas = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? ["http://localhost:3000"];

            services.AddCors(options =>
            {
                options.AddPolicy(PoliticaFrontEnd, policy =>
                {
                    policy
                        .WithOrigins(origensPermitidas)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }
    }
}