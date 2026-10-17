using API.IntegrationTests.Factories;
using Domain.Security.Criptografia;
using Infrastructure.DataAcess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;
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
    }
}
