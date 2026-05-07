using Infrastructure.DataAcess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests.Factories
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName;

        public CustomWebApplicationFactory(string dbName)
        {
            _dbName = dbName;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Limpa TODAS as fontes anteriores (appsettings.json, appsettings.Development.json, etc)
                // para garantir que só o Testing seja usado
                config.Sources.Clear();

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                config.AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: false);
                config.AddEnvironmentVariables();
            });

            builder.ConfigureServices(services =>
            {
                // Remove o DbContext original (PostgreSQL)
                var descriptor = services.SingleOrDefault(
                    s => s.ServiceType == typeof(DbContextOptions<EccomerceDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Substitui por InMemory com banco isolado por teste
                services.AddDbContext<EccomerceDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });
            });

            builder.UseEnvironment("Testing");
        }
    }
}
