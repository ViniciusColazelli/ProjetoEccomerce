using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace API.IntegrationTests.Factories
{
    // Usa o PostgreSQL real (Supabase de testes) configurado em appsettings.Testing.json,
    // para que CHECK, índices únicos e FKs sejam validados nos testes.
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Limpa TODAS as fontes anteriores (appsettings.json, appsettings.Development.json, etc)
                // para garantir que só o Testing seja usado
                config.Sources.Clear();

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                config.AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: false);

                // No CI a connection string vem de ConnectionStrings__ConnectionPostgreSql
                config.AddEnvironmentVariables();
            });

            builder.UseEnvironment("Testing");
        }
    }
}
