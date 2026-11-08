using API.IntegrationTests.Factories;
using API.IntegrationTests.Fixture;
using Application.Request;
using Application.Response;
using Domain.Entities;
using Domain.Security.Perfis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    // Fotos gravadas no disco (Storage:Provedor=Local na CustomWebApplicationFactory)
    public class AdminProdutoImagemTests : BaseTesteIntegracao
    {
        public AdminProdutoImagemTests(EcommerceFixture fixture) : base(fixture) { }

        private static string Endpoint(int produtoId) => $"/api/admin/produto/{produtoId}/imagem";

        // só a assinatura de cada formato importa: a API não decodifica a imagem
        private static readonly byte[] WEBP = [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBPVP8 "u8, 1, 2, 3, 4];
        private static readonly byte[] JPEG = [0xFF, 0xD8, 0xFF, 0xE0, 0, 16, .. "JFIF"u8, 1, 2, 3];
        private static readonly byte[] PNG = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 1, 2];

        // ════════════════════════════════════════════════════════════════
        // AUTORIZAÇÃO
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return401_When_NaoLogado()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var produto = await PrepararProduto(ct);

            // Act
            var response = await _fixture.Client.PostAsync(Endpoint(produto.Id), Formulario(WEBP, WEBP), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_Return403_When_PerfilCliente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var produto = await PrepararProduto(ct);

            // Act
            var response = await client.PostAsync(Endpoint(produto.Id), Formulario(WEBP, WEBP), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════
        // ENVIAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return201AndGravarArquivos_When_FotoValida()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct);

            // Act
            var response = await client.PostAsync(Endpoint(produto.Id), Formulario(WEBP, WEBP), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutoImagem>(ct);
            body!.Ordem.Should().Be(0, because: "a primeira foto é a capa");
            body.Url.Should().StartWith($"{CustomWebApplicationFactory.UrlPublicaImagens}/produtos/{produto.Id}/").And.EndWith(".webp");
            body.UrlMiniatura.Should().EndWith("-mini.webp");

            using var scope = _fixture.CriarScope();
            var salva = await _fixture.GetDbContext(scope).ProdutoImagens.SingleAsync(ct);
            salva.Id.Should().Be(body.Id);
            salva.ProdutoId.Should().Be(produto.Id);

            ArquivoGravado(salva.Caminho).Should().Equal(WEBP);
            ArquivoGravado(salva.CaminhoMiniatura).Should().Equal(WEBP);

            // e a própria API serve a foto no endereço devolvido
            var foto = await _fixture.Client.GetAsync(new Uri(body.Url).AbsolutePath, ct);
            foto.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // a extensão e o Content-Type gravados vêm do conteúdo, não do nome que o navegador mandou
        [Theory]
        [InlineData("jpeg", ".jpg")]
        [InlineData("png", ".png")]
        public async Task Should_AceitarJpegEPng(string formato, string extensaoEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct);
            var bytes = formato == "jpeg" ? JPEG : PNG;

            // Act
            var response = await client.PostAsync(Endpoint(produto.Id), Formulario(bytes, bytes, nomeArquivo: "foto.webp"), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutoImagem>(ct);
            body!.Url.Should().EndWith(extensaoEsperada);
            body.UrlMiniatura.Should().EndWith($"-mini{extensaoEsperada}");
        }

        [Fact]
        public async Task Should_ColocarNovaFotoDepoisDasExistentes()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct, ImagemNoBanco(ordem: 0), ImagemNoBanco(ordem: 4));

            // Act
            var response = await client.PostAsync(Endpoint(produto.Id), Formulario(WEBP, WEBP), ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseProdutoImagem>(ct);
            body!.Ordem.Should().Be(5);
        }

        [Fact]
        public async Task Should_Return400_When_ArquivoNaoEImagem()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct);
            var texto = Encoding.UTF8.GetBytes("isto não é uma foto, só tem o nome de uma");

            // Act
            var response = await client.PostAsync(Endpoint(produto.Id), Formulario(texto, WEBP, nomeArquivo: "foto.webp"), ct);

            // Assert
            await DeveSerErro(response, "A foto precisa ser JPEG, PNG ou WebP.", ct);
            await NadaGravado(ct);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task Should_Return400_When_FaltaArquivo(bool comImagem, bool comMiniatura)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct);

            // Act
            var response = await client.PostAsync(Endpoint(produto.Id), Formulario(comImagem ? WEBP : null, comMiniatura ? WEBP : null), ct);

            // Assert
            await DeveSerErro(response, "Envie a foto e a miniatura.", ct);
        }

        [Fact]
        public async Task Should_Return400_When_FotoMaiorQue3MB()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct);
            var grande = new byte[3 * 1024 * 1024 + 1];
            WEBP.CopyTo(grande, 0);

            // Act
            var response = await client.PostAsync(Endpoint(produto.Id), Formulario(grande, WEBP), ct);

            // Assert
            await DeveSerErro(response, "A foto deve ter no máximo 3 MB e a miniatura, 1 MB.", ct);
            await NadaGravado(ct);
        }

        [Fact]
        public async Task Should_Return400_When_ProdutoJaTem8Fotos()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct, Enumerable.Range(0, 8).Select(ordem => ImagemNoBanco(ordem)).ToArray());

            // Act
            var response = await client.PostAsync(Endpoint(produto.Id), Formulario(WEBP, WEBP), ct);

            // Assert
            await DeveSerErro(response, "O produto já tem 8 fotos, o máximo permitido. Apague uma antes de enviar outra.", ct);

            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).ProdutoImagens.CountAsync(ct)).Should().Be(8);
        }

        [Fact]
        public async Task Should_Return404_When_ProdutoNaoExiste()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.PostAsync(Endpoint(999999), Formulario(WEBP, WEBP), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════
        // REMOVER
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return204AndApagarArquivos_When_Remover()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct);
            var enviada = await (await client.PostAsync(Endpoint(produto.Id), Formulario(WEBP, WEBP), ct))
                .Content.ReadFromJsonAsync<ResponseProdutoImagem>(ct);

            string caminho, caminhoMiniatura;
            using (var scopeAntes = _fixture.CriarScope())
            {
                var gravada = await _fixture.GetDbContext(scopeAntes).ProdutoImagens.SingleAsync(ct);
                (caminho, caminhoMiniatura) = (gravada.Caminho, gravada.CaminhoMiniatura);
            }

            // Act
            var response = await client.DeleteAsync($"{Endpoint(produto.Id)}/{enviada!.Id}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).ProdutoImagens.AnyAsync(ct)).Should().BeFalse();
            File.Exists(CaminhoNoDisco(caminho)).Should().BeFalse();
            File.Exists(CaminhoNoDisco(caminhoMiniatura)).Should().BeFalse();
        }

        [Fact]
        public async Task Should_Return404_When_FotoEDeOutroProduto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var foto = ImagemNoBanco(ordem: 0);
            await PrepararProduto(ct, foto);
            var outro = await PrepararProduto(ct);

            // Act
            var response = await client.DeleteAsync($"{Endpoint(outro.Id)}/{foto.Id}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).ProdutoImagens.CountAsync(ct)).Should().Be(1);
        }

        // ════════════════════════════════════════════════════════════════
        // ORDENAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return200AndRenumerar_When_OrdemCompleta()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var a = ImagemNoBanco(ordem: 0);
            var b = ImagemNoBanco(ordem: 3);
            var c = ImagemNoBanco(ordem: 7);
            var produto = await PrepararProduto(ct, a, b, c);

            // Act
            var response = await client.PutAsJsonAsync($"{Endpoint(produto.Id)}/ordem", new RequestOrdenarImagens { Ids = [c.Id, a.Id, b.Id] }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<List<ResponseProdutoImagem>>(ct);
            body!.Select(i => (i.Id, i.Ordem)).Should().Equal((c.Id, 0), (a.Id, 1), (b.Id, 2));

            using var scope = _fixture.CriarScope();
            var salvas = await _fixture.GetDbContext(scope).ProdutoImagens.OrderBy(i => i.Ordem).Select(i => i.Id).ToListAsync(ct);
            salvas.Should().Equal(c.Id, a.Id, b.Id);
        }

        [Theory]
        [InlineData("faltando")]
        [InlineData("repetida")]
        [InlineData("de outro produto")]
        public async Task Should_Return400_When_OrdemIncompleta(string caso)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var a = ImagemNoBanco(ordem: 0);
            var b = ImagemNoBanco(ordem: 1);
            var produto = await PrepararProduto(ct, a, b);
            var deOutro = ImagemNoBanco(ordem: 0);
            await PrepararProduto(ct, deOutro);

            List<int> ids = caso switch
            {
                "faltando" => [a.Id],
                "repetida" => [a.Id, a.Id],
                _ => [a.Id, deOutro.Id]
            };

            // Act
            var response = await client.PutAsJsonAsync($"{Endpoint(produto.Id)}/ordem", new RequestOrdenarImagens { Ids = ids }, ct);

            // Assert
            await DeveSerErro(response, "Informe todas as fotos do produto, cada uma uma vez só.", ct);
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        private async Task<Produtos> PrepararProduto(CancellationToken ct, params ProdutoImagens[] imagens)
        {
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            var produto = new Produtos { Nome = "Camiseta Polo", PrecoBase = 59.90m, Ativo = true };
            foreach (var imagem in imagens)
                produto.Imagens.Add(imagem);

            await dbContext.Produtos.AddAsync(produto, ct);
            await dbContext.SaveChangesAsync(ct);

            return produto;
        }

        // registro direto no banco, sem arquivo: basta para os testes de limite, remoção e ordem
        private static ProdutoImagens ImagemNoBanco(int ordem) => new()
        {
            Caminho = $"produtos/teste/{Guid.NewGuid():N}.webp",
            CaminhoMiniatura = $"produtos/teste/{Guid.NewGuid():N}-mini.webp",
            Ordem = ordem
        };

        private static MultipartFormDataContent Formulario(byte[]? imagem, byte[]? miniatura, string nomeArquivo = "foto.webp")
        {
            var formulario = new MultipartFormDataContent();

            if (imagem is not null)
                formulario.Add(Arquivo(imagem), "imagem", nomeArquivo);
            if (miniatura is not null)
                formulario.Add(Arquivo(miniatura), "miniatura", "mini-" + nomeArquivo);

            return formulario;
        }

        private static ByteArrayContent Arquivo(byte[] bytes)
        {
            var conteudo = new ByteArrayContent(bytes);
            conteudo.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
            return conteudo;
        }

        private static string CaminhoNoDisco(string caminho) =>
            Path.Combine(CustomWebApplicationFactory.PastaImagens, caminho.Replace('/', Path.DirectorySeparatorChar));

        private static byte[] ArquivoGravado(string caminho) => File.ReadAllBytes(CaminhoNoDisco(caminho));

        private static async Task DeveSerErro(HttpResponseMessage response, string mensagem, CancellationToken ct)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors").EnumerateArray().Select(e => e.GetString()).Should().Contain(mensagem);
        }

        private async Task NadaGravado(CancellationToken ct)
        {
            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).ProdutoImagens.AnyAsync(ct)).Should().BeFalse();
        }
    }
}
