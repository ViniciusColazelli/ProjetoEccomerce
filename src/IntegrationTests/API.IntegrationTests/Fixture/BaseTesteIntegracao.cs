using Xunit;

namespace API.IntegrationTests.Fixture
{
    // Toda classe de teste herda daqui: o banco é limpo antes de cada teste,
    // então nenhum teste depende do que outro deixou gravado.
    public abstract class BaseTesteIntegracao : IAsyncLifetime
    {
        protected readonly EcommerceFixture _fixture;

        protected BaseTesteIntegracao(EcommerceFixture fixture)
        {
            _fixture = fixture;
        }

        public async ValueTask InitializeAsync()
        {
            await _fixture.ResetarBanco();
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
