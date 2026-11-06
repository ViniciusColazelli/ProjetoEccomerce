using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace API.IntegrationTests.Factories
{
    // Usa um PostgreSQL real (o de testes do appsettings.Testing.json, ou o container do GitHub Actions),
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

                // Na máquina de quem desenvolve as configurações vêm do appsettings.Testing.json (fora do Git);
                // no GitHub Actions o arquivo não existe e tudo vem das variáveis de ambiente do workflow
                config.AddJsonFile("appsettings.Testing.json", optional: true, reloadOnChange: false);

                // BCrypt com o custo mínimo: a suíte cadastra muitos clientes e o custo de produção (11) a deixaria lenta
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Settings:Password:BCryptFatorCusto"] = "4" });

                // Por último, para valer sobre o arquivo: ConnectionStrings__ConnectionPostgreSql, Jwt__ChaveSecreta...
                config.AddEnvironmentVariables();
            });

            builder.UseEnvironment("Testing");
        }
    }
}
