using Domain.Repositories;
using Domain.Security.Criptografia;
using Infrastructure.DataAcess;
using Infrastructure.DataAcess.Repositories;
using Infrastructure.Security.Criptografia;
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
            AddRepositories(services);
            SenhaCriptografada(services, configuration);
        }

        private static void AddDbContext_PostgreSql(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<EccomerceDbContext>(dbContextOptions =>
            {
                dbContextOptions.UseNpgsql(configuration.GetConnectionString("ConnectionPostgreSql"));
            });
        }

        private static void AddRepositories(IServiceCollection services)
        {
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<ISalvarDBRepository, SalvarDBRepository>();
        }

        private static void SenhaCriptografada(IServiceCollection services, IConfiguration configuration)
        {
            var additionalKey = configuration.GetValue<string>("Settings:Password:AdditionalKey");

            services.AddScoped<ISenhaCriptografada>(options => new Sha512Encripter(additionalKey!));
        }
    }
}
