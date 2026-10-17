using API.IntegrationTests.Fixture;
using Xunit;

// Uma única API (e um único banco) para toda a suíte
[assembly: AssemblyFixture(typeof(EcommerceFixture))]

// O banco de testes é compartilhado: testes em paralelo apagariam os dados uns dos outros
[assembly: CollectionBehavior(DisableTestParallelization = true)]
