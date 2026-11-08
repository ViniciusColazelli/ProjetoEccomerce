using API.IntegrationTests.Factories;
using API.IntegrationTests.Fixture;
using Application.Request;
using Application.Response;
using Domain.Entities;
using Domain.Security.Perfis;
using FluentAssertions;
using System.Net.Http.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    // As fotos aparecem nas respostas de produto: só a miniatura da capa nas listas
    // (vitrine e admin) e a galeria inteira, em ordem, nos detalhes
    public class FotosNasRespostasTests : BaseTesteIntegracao
    {
        public FotosNasRespostasTests(EcommerceFixture fixture) : base(fixture) { }

        private static string Url(string caminho) => $"{CustomWebApplicationFactory.UrlPublicaImagens}/{caminho}";

        // ════════════════════════════════════════════════════════════════
        // VITRINE
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_MostrarMiniaturaDaCapaNaVitrine_E_NullSemFoto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var costas = Foto("costas", ordem: 1);
            var frente = Foto("frente", ordem: 0);
            await PrepararProdutos(ct, NovoProduto("Camiseta Polo", costas, frente), NovoProduto("Jaleco"));

            // Act
            var body = await _fixture.Client.GetFromJsonAsync<ResponseProdutos>("/api/produto", ct);

            // Assert: a capa é a de menor ordem, e mais de uma foto não repete o produto na lista
            body!.Total.Should().Be(2);
            body.Produtos.Should().HaveCount(2);
            body.Produtos.Single(p => p.Nome == "Camiseta Polo").ImagemUrl.Should().Be(Url(frente.CaminhoMiniatura));
            body.Produtos.Single(p => p.Nome == "Jaleco").ImagemUrl.Should().BeNull(because: "sem foto o front mostra o placeholder");
        }

        [Fact]
        public async Task Should_MostrarGaleriaEmOrdemNoDetalheDaVitrine()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var costas = Foto("costas", ordem: 1);
            var frente = Foto("frente", ordem: 0);
            var produto = NovoProduto("Camiseta Polo", costas, frente);
            await PrepararProdutos(ct, produto);

            // Act
            var body = await _fixture.Client.GetFromJsonAsync<ResponseProdutoDetalhe>($"/api/produto/{produto.Id}", ct);

            // Assert
            body!.Imagens.Select(i => (i.Id, i.Url, i.UrlMiniatura, i.Ordem)).Should().Equal(
                (frente.Id, Url(frente.Caminho), Url(frente.CaminhoMiniatura), 0),
                (costas.Id, Url(costas.Caminho), Url(costas.CaminhoMiniatura), 1));
        }

        [Fact]
        public async Task Should_DevolverGaleriaVazia_When_ProdutoSemFoto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var produto = NovoProduto("Jaleco");
            await PrepararProdutos(ct, produto);

            // Act
            var body = await _fixture.Client.GetFromJsonAsync<ResponseProdutoDetalhe>($"/api/produto/{produto.Id}", ct);

            // Assert
            body!.Imagens.Should().BeEmpty();
        }

        // ════════════════════════════════════════════════════════════════
        // ADMIN
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_MostrarCapaNaListaDoAdmin()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var frente = Foto("frente", ordem: 0);
            await PrepararProdutos(ct, NovoProduto("Camiseta Polo", frente, Foto("costas", ordem: 1)), NovoProduto("Jaleco", ativo: false));

            // Act
            var body = await client.GetFromJsonAsync<ResponseProdutosAdmin>("/api/admin/produto", ct);

            // Assert
            body!.Produtos.Single(p => p.Nome == "Camiseta Polo").ImagemUrl.Should().Be(Url(frente.CaminhoMiniatura));
            body.Produtos.Single(p => p.Nome == "Jaleco").ImagemUrl.Should().BeNull();
        }

        [Fact]
        public async Task Should_MostrarGaleriaNoDetalheDoAdmin()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var costas = Foto("costas", ordem: 1);
            var frente = Foto("frente", ordem: 0);
            var produto = NovoProduto("Camiseta Polo", costas, frente);
            await PrepararProdutos(ct, produto);

            // Act
            var body = await client.GetFromJsonAsync<ResponseProdutoAdmin>($"/api/admin/produto/{produto.Id}", ct);

            // Assert
            body!.Imagens.Select(i => i.Id).Should().Equal(frente.Id, costas.Id);
            body.Imagens[0].Url.Should().Be(Url(frente.Caminho));
        }

        // Mudar a ordem no admin troca a capa que a vitrine mostra
        [Fact]
        public async Task Should_TrocarCapaDaVitrine_When_OrdemMuda()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var frente = Foto("frente", ordem: 0);
            var costas = Foto("costas", ordem: 1);
            var produto = NovoProduto("Camiseta Polo", frente, costas);
            await PrepararProdutos(ct, produto);

            // Act
            var ordem = await client.PutAsJsonAsync($"/api/admin/produto/{produto.Id}/imagem/ordem", new RequestOrdenarImagens { Ids = [costas.Id, frente.Id] }, ct);
            var vitrine = await _fixture.Client.GetFromJsonAsync<ResponseProdutos>("/api/produto", ct);

            // Assert
            ordem.EnsureSuccessStatusCode();
            vitrine!.Produtos.Single().ImagemUrl.Should().Be(Url(costas.CaminhoMiniatura));
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        private async Task PrepararProdutos(CancellationToken ct, params Produtos[] produtos)
        {
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            await dbContext.Produtos.AddRangeAsync(produtos, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        private static Produtos NovoProduto(string nome, params ProdutoImagens[] imagens) => NovoProduto(nome, ativo: true, imagens);

        private static Produtos NovoProduto(string nome, bool ativo, params ProdutoImagens[] imagens)
        {
            var produto = new Produtos { Nome = nome, PrecoBase = 59.90m, Ativo = ativo };
            foreach (var imagem in imagens)
                produto.Imagens.Add(imagem);
            return produto;
        }

        // só o registro: as respostas montam a URL a partir do caminho, sem abrir o arquivo
        private static ProdutoImagens Foto(string nome, int ordem) => new()
        {
            Caminho = $"produtos/teste/{nome}-{Guid.NewGuid():N}.webp",
            CaminhoMiniatura = $"produtos/teste/{nome}-{Guid.NewGuid():N}-mini.webp",
            Ordem = ordem
        };
    }
}
