using Domain.Repositories;
using Domain.Security.Criptografia;
using Domain.Security.Tokens;
using Domain.Services.ClienteLogado;
using Exceptions;
using FluentMigrator.Runner;
using Infrastructure.DataAcess;
using Infrastructure.DataAcess.Repositories;
using Infrastructure.Security.Criptografia;
using Infrastructure.Security.Tokens;
using Infrastructure.Services.ClienteLogado;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Reflection;
using System.Text;

namespace Infrastructure
{
    public static class InjecaoDeDependenciaExtension
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            SenhaCriptografada(services);
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
                dbContextOptions.UseNpgsql(ConnectionStringPostgreSql(configuration));
            });
        }

        // O Npgsql 9+ tenta criptografia GSS (Kerberos) a cada conexão nova. A imagem Docker do .NET 10 (Ubuntu)
        // não tem a libgssapi, então a tentativa só gerava "Cannot load library libgssapi_krb5.so.2" no log;
        // o Supabase não usa Kerberos (a conexão já é criptografada pelo SSL)
        private static string? ConnectionStringPostgreSql(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("ConnectionPostgreSql");

            if (string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            return new NpgsqlConnectionStringBuilder(connectionString)
            {
                GssEncryptionMode = GssEncryptionMode.Disable
            }.ConnectionString;
        }

        private static void AddFluentMigrator(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = ConnectionStringPostgreSql(configuration);

            services.AddFluentMigratorCore().ConfigureRunner(options => options
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations());
        }

        private static void AddRepositories(IServiceCollection services)
        {
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<IProdutoRepository, ProdutoRepository>();
            services.AddScoped<IProdutoOpcaoRepository, ProdutoOpcaoRepository>();
            services.AddScoped<IProdutoVarianteRepository, ProdutoVarianteRepository>();
            services.AddScoped<IEstoqueRepository, EstoqueRepository>();
            services.AddScoped<IEstoqueMovimentacaoRepository, EstoqueMovimentacaoRepository>();
            services.AddScoped<IPedidoRepository, PedidoRepository>();
            services.AddScoped<ISalvarDBRepository, SalvarDBRepository>();
        }

        private static void AddClienteLogado(IServiceCollection services)
        {
            services.AddScoped<IClienteLogado, ClienteLogado>();
        }

        private static void SenhaCriptografada(IServiceCollection services)
        {
            // singleton: o hash fictício do BCrypt é calculado uma vez só.
            // A configuração é lida na criação do serviço (e não aqui) para valer a que os testes injetam no WebApplicationFactory.
            services.AddSingleton<ISenhaCriptografada>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var fatorCusto = config.GetValue("Settings:Password:BCryptFatorCusto", BCryptSenha.FATOR_CUSTO_PADRAO);

                return new BCryptSenha(fatorCusto);
            });
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

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Authorization: Bearer continua valendo (Postman, testes); sem ele, lê o cookie
                        if (!context.Request.Headers.ContainsKey("Authorization") &&
                            context.Request.Cookies.TryGetValue(CookieAutenticacao.Nome, out var token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                    // 401: sem token, token inválido ou expirado
                    OnChallenge = async context =>
                    {
                        context.HandleResponse(); // evita a resposta padrão, de corpo vazio

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(
                            new { errors = new[] { ResourceMensagensDeErro.CLIENTE_NAO_LOGADO } });
                    },

                    // 403: logado, mas sem o perfil exigido
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(
                            new { errors = new[] { ResourceMensagensDeErro.ACESSO_NEGADO } });
                    }
                };
            });

            services.AddAuthorization();
        }
    }
}
