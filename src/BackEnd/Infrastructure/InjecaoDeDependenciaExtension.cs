using Domain.Repositories;
using Domain.Security.Criptografia;
using Domain.Services.ClienteLogado;
using FluentMigrator.Runner;
using Infrastructure.DataAcess;
using Infrastructure.DataAcess.Repositories;
using Infrastructure.Security.Criptografia;
using Infrastructure.Services.ClienteLogado;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Infrastructure
{
    public static class InjecaoDeDependenciaExtension
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            SenhaCriptografada(services, configuration);
            AddDbContext_PostgreSql(services, configuration);
            AddFluentMigrator(services, configuration);
            AddRepositories(services);
            AddClienteLogado(services);
        }

        private static void AddDbContext_PostgreSql(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<EccomerceDbContext>(dbContextOptions =>
            {
                dbContextOptions.UseNpgsql(configuration.GetConnectionString("ConnectionPostgreSql"));
            });
        }

        private static void AddFluentMigrator(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("ConnectionPostgreSql");

            services.AddFluentMigratorCore().ConfigureRunner(options => options
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations());
        }

        private static void AddRepositories(IServiceCollection services)
        {
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<ISalvarDBRepository, SalvarDBRepository>();
        }

        private static void AddClienteLogado(IServiceCollection services)
        {
            services.AddScoped<IClienteLogado, ClienteLogado>();
        }

        private static void SenhaCriptografada(IServiceCollection services, IConfiguration configuration)
        {
            var additionalKey = configuration.GetValue<string>("Settings:Password:AdditionalKey");

            services.AddScoped<ISenhaCriptografada>(options => new Sha512Encripter(additionalKey!));
        }
    }
}
