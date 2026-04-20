using Infrastructure.DataAcess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class InjecaoDeDependenciaExtension
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            AddDbContext_PostgreSql(services, configuration);
        }

        private static void AddDbContext_PostgreSql(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<EccomerceDbContext>(dbContextOptions =>
            {
                dbContextOptions.UseNpgsql(configuration.GetConnectionString("ConnectionPostgreSql"));
            });
        }
    }
}
