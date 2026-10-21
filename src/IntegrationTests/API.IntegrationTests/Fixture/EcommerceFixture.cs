using API.IntegrationTests.Factories;
using Domain.Entities;
using Domain.Security.Criptografia;
using Domain.Security.Tokens;
using Infrastructure.DataAcess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace API.IntegrationTests.Fixture
{
    // Fixture de assembly: a API sobe uma única vez para toda a suíte,
    // e o banco é limpo antes de cada teste (ver BaseTesteIntegracao).
    public class EcommerceFixture : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        private Respawner _respawner = null!;
        private string _connectionString = string.Empty;

        public HttpClient Client { get; private set; } = null!;

        // mesmas opções da API (Program.cs): enums trafegam pelo nome
        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        // ── Chamado UMA vez, antes do primeiro teste ──────────────────
        public async ValueTask InitializeAsync()
        {
            _factory = new CustomWebApplicationFactory();

            // Criar o client sobe a API, e o Program.cs roda as migrations
            Client = _factory.CreateClient();

            _connectionString = _factory.Services
                .GetRequiredService<IConfiguration>()
                .GetConnectionString("ConnectionPostgreSql")!;

            await using var conexao = new NpgsqlConnection(_connectionString);
            await conexao.OpenAsync();

            _respawner = await Respawner.CreateAsync(conexao, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],

                // Tabela de controle do FluentMigrator: apagá-la faria as migrations rodarem de novo
                TablesToIgnore = [new Table("public", "VersionInfo")]
            });
        }

        // ── Chamado UMA vez, depois do último teste ───────────────────
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
        }

        // ── Helpers ───────────────────────────────────────────────────
        public async Task ResetarBanco()
        {
            await using var conexao = new NpgsqlConnection(_connectionString);
            await conexao.OpenAsync();

            await _respawner.ResetAsync(conexao);
        }

        public IServiceScope CriarScope() =>
            _factory.Services.CreateScope();

        public EccomerceDbContext GetDbContext(IServiceScope scope) =>
            scope.ServiceProvider.GetRequiredService<EccomerceDbContext>();

        public ISenhaCriptografada GetCriptografia(IServiceScope scope) =>
            scope.ServiceProvider.GetRequiredService<ISenhaCriptografada>();

        // Grava um cliente com o perfil informado e devolve um HttpClient já autenticado com o token dele.
        // Chamar dentro do teste: o banco é limpo antes de cada um.
        public async Task<HttpClient> CriarClientAutenticado(string perfil)
        {
            using var scope = CriarScope();
            var dbContext = GetDbContext(scope);

            var cliente = new Clientes
            {
                Nome = $"Teste {perfil}",
                Email = $"{Guid.NewGuid():N}@teste.com",
                Senha = "nao-utilizada",
                Perfil = perfil
            };

            dbContext.Clientes.Add(cliente);
            await dbContext.SaveChangesAsync();

            var token = scope.ServiceProvider.GetRequiredService<IGeradorTokenJwt>().GerarToken(cliente);

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }
    }
}
