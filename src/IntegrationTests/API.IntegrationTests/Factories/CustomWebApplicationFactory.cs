using Infrastructure.DataAcess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
            // Decisão: sobrescrevemos apenas o DbContext aqui.
            // Todos os outros serviços reais da aplicação (UseCase, Repository,
            // Criptografia, FluentValidation) continuam sendo usados — sem mocks.
            builder.ConfigureServices(services =>
            {
                // Remove o registro do DbContext original (PostgreSQL)
                var descriptor = services.SingleOrDefault(
                    s => s.ServiceType == typeof(DbContextOptions<EccomerceDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Registra o DbContext com InMemory usando nome único por teste
                services.AddDbContext<EccomerceDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });
            });

            // Garante que o ambiente seja "Development" para ativar Swagger e tratamento de erro
            builder.UseEnvironment("Development");
        }
    }
}
