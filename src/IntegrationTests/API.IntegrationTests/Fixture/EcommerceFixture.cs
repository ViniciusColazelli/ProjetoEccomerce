using API.IntegrationTests.Factories;
using Application.Request;
using Domain.Entities;
using Domain.Security.Criptografia;
using Domain.Security.Tokens;
using Infrastructure.DataAcess;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

        // Client que não guarda cookies sozinho: o teste lê o Set-Cookie e decide quando enviar o cookie
        public HttpClient CriarClientSemCookies() =>
            _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        // Grava um cliente com a senha criptografada de verdade (BCrypt), para os testes que passam pelo login
        public async Task<Clientes> CadastrarCliente(string email, string senha, string nome = "Cliente Teste")
        {
            using var scope = CriarScope();

            return await GravarCliente(email, GetCriptografia(scope).Gerar(senha), nome);
        }

        // Grava o hash exatamente como informado (ex.: um hash no formato antigo, que não é mais aceito)
        public Task<Clientes> CadastrarClienteComHash(string email, string hashSenha, string nome = "Cliente Teste") =>
            GravarCliente(email, hashSenha, nome);

        private async Task<Clientes> GravarCliente(string email, string hashSenha, string nome)
        {
            using var scope = CriarScope();
            var dbContext = GetDbContext(scope);

            var cliente = new Clientes
            {
                Nome = nome,
                Email = email,
                Senha = hashSenha
            };

            dbContext.Clientes.Add(cliente);
            await dbContext.SaveChangesAsync();

            return cliente;
        }

        public async Task<string> SenhaGravada(int clienteId)
        {
            using var scope = CriarScope();

            return await GetDbContext(scope).Clientes.Where(c => c.Id == clienteId).Select(c => c.Senha).SingleAsync();
        }

        // Faz login de verdade e devolve um client que guarda o cookie, como o navegador
        public async Task<HttpClient> CriarClientLogado(string email, string senha)
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/login", new RequestLoginCliente { Email = email, Senha = senha });
            response.EnsureSuccessStatusCode();

            return client;
        }

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
