using Domain.Repositories;
using Domain.Security.Criptografia;
using Domain.Security.Tokens;
using Domain.Services.ClienteLogado;
using FluentMigrator.Runner;
using Infrastructure.DataAcess;
using Infrastructure.DataAcess.Repositories;
using Infrastructure.Security.Criptografia;
using Infrastructure.Security.Tokens;
using Infrastructure.Services.ClienteLogado;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

namespace Infrastructure
{
    public static class InjecaoDeDependenciaExtension
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            SenhaCriptografada(services, configuration);
            AddTokenJwt(services);
            AddAuthenticationJwt(services, configuration);
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

        private static void AddTokenJwt(IServiceCollection services)
        {
            services.AddScoped<IGeradorTokenJwt, GeradorTokenJwt>();
        }

        private static void AddAuthenticationJwt(IServiceCollection services, IConfiguration configuration)
        {
            var chaveSecreta = configuration.GetValue<string>("Jwt:ChaveSecreta")!;
            var issuer = configuration.GetValue<string>("Jwt:Issuer")!;
            var audience = configuration.GetValue<string>("Jwt:Audience")!;

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveSecreta))
                };
            });

            services.AddAuthorization();
        }
    }
}
