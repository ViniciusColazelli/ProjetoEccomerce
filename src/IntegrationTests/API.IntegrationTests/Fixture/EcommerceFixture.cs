using API.IntegrationTests.Factories;
using Domain.Security.Criptografia;
using Infrastructure.DataAcess;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests.Fixture
{
    public class EcommerceFixture : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        public HttpClient Client { get; private set; } = null!;

        // ── Chamado ANTES de cada teste ───────────────────────────────
        public async ValueTask InitializeAsync()
        {
            // Guid único garante banco novo e isolado para cada teste
            _factory = new CustomWebApplicationFactory(dbName: Guid.NewGuid().ToString());
            Client = _factory.CreateClient();
            await Task.CompletedTask;
        }

        // ── Chamado DEPOIS de cada teste ──────────────────────────────
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
        }

        // ── Helpers ───────────────────────────────────────────────────
        public IServiceScope CriarScope() =>
            _factory.Services.CreateScope();

        public EccomerceDbContext GetDbContext(IServiceScope scope) =>
            scope.ServiceProvider.GetRequiredService<EccomerceDbContext>();

        public ISenhaCriptografada GetCriptografia(IServiceScope scope) =>
            scope.ServiceProvider.GetRequiredService<ISenhaCriptografada>();
    }
}
