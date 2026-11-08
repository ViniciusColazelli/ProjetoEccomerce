using API.IntegrationTests.Factories;
using API.IntegrationTests.Fixture;
using Domain.Services.Armazenamento;
using FluentAssertions;
using Infrastructure.Services.Armazenamento;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    // Os dois lugares onde as fotos podem ficar: disco local (Development e testes) e Supabase Storage.
    // Nenhum teste fala com o Supabase de verdade: o HTTP dele é simulado.
    public class ArmazenamentoImagensTests
    {
        private readonly EcommerceFixture _fixture;

        public ArmazenamentoImagensTests(EcommerceFixture fixture)
        {
            _fixture = fixture;
        }

        // ════════════════════════════════════════════════════════════════
        // DISCO LOCAL
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_GravarNoDiscoEMontarUrl_When_Local()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var pasta = NovaPasta();
            var armazenamento = new ArmazenamentoLocal(pasta, "http://localhost/imagens/");

            // Act
            await armazenamento.Salvar("produtos/7/foto.webp", Conteudo("abc"), "image/webp", ct);

            // Assert
            File.ReadAllText(Path.Combine(pasta, "produtos", "7", "foto.webp")).Should().Be("abc");
            armazenamento.UrlPublica("produtos/7/foto.webp").Should().Be("http://localhost/imagens/produtos/7/foto.webp");
        }

        [Fact]
        public async Task Should_NaoSobrescrever_When_ArquivoJaExiste()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var pasta = NovaPasta();
            var armazenamento = new ArmazenamentoLocal(pasta, "http://localhost/imagens");
            await armazenamento.Salvar("foto.webp", Conteudo("original"), "image/webp", ct);

            // Act
            var acao = () => armazenamento.Salvar("foto.webp", Conteudo("outra"), "image/webp", ct);

            // Assert
            await acao.Should().ThrowAsync<IOException>();
            File.ReadAllText(Path.Combine(pasta, "foto.webp")).Should().Be("original");
        }

        [Fact]
        public async Task Should_ApagarDoDisco_E_IgnorarArquivoQueNaoExiste()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var pasta = NovaPasta();
            var armazenamento = new ArmazenamentoLocal(pasta, "http://localhost/imagens");
            await armazenamento.Salvar("produtos/1/a.webp", Conteudo("a"), "image/webp", ct);

            // Act
            await armazenamento.Apagar(["produtos/1/a.webp", "produtos/1/nao-existe.webp"], ct);

            // Assert
            File.Exists(Path.Combine(pasta, "produtos", "1", "a.webp")).Should().BeFalse();
        }

        // um caminho assim gravaria ou apagaria fora da pasta das fotos
        [Theory]
        [InlineData("../fora.webp")]
        [InlineData("produtos/../../fora.webp")]
        [InlineData("/raiz.webp")]
        [InlineData("produtos\\1\\a.webp")]
        [InlineData("")]
        public async Task Should_RecusarCaminhoInvalido(string caminho)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var armazenamento = new ArmazenamentoLocal(NovaPasta(), "http://localhost/imagens");

            // Act
            var salvar = () => armazenamento.Salvar(caminho, Conteudo("x"), "image/webp", ct);
            var apagar = () => armazenamento.Apagar([caminho], ct);

            // Assert
            await salvar.Should().ThrowAsync<ArgumentException>();
            await apagar.Should().ThrowAsync<ArgumentException>();
        }

        // A API configurada nos testes usa o disco e serve as fotos em /imagens, como em Development
        [Fact]
        public async Task Should_ServirFotoPelaApi_When_ProvedorLocal()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var caminho = $"produtos/teste/{Guid.NewGuid():N}.webp";

            using (var scope = _fixture.CriarScope())
            {
                var armazenamento = scope.ServiceProvider.GetRequiredService<IArmazenamentoImagens>();
                armazenamento.Should().BeOfType<ArmazenamentoLocal>();

                await armazenamento.Salvar(caminho, Conteudo("webp-falso"), "image/webp", ct);
                armazenamento.UrlPublica(caminho).Should().Be($"{CustomWebApplicationFactory.UrlPublicaImagens}/{caminho}");
            }

            // Act
            var response = await _fixture.Client.GetAsync($"/imagens/{caminho}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
            (await response.Content.ReadAsStringAsync(ct)).Should().Be("webp-falso");
            File.Exists(Path.Combine(CustomWebApplicationFactory.PastaImagens, "produtos", "teste", Path.GetFileName(caminho))).Should().BeTrue();
        }

        // ════════════════════════════════════════════════════════════════
        // SUPABASE STORAGE (HTTP simulado)
        // ════════════════════════════════════════════════════════════════

        private const string CHAVE = "chave-de-servico-de-teste";

        [Fact]
        public async Task Should_EnviarArquivoAoSupabase_ComChaveESemSobrescrever()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var http = new HttpFalso(HttpStatusCode.OK);
            var armazenamento = new ArmazenamentoSupabase(new HttpClient(http), ConfigSupabase());

            // Act
            await armazenamento.Salvar("produtos/7/foto.webp", Conteudo("abc"), "image/webp", ct);

            // Assert
            var requisicao = http.Requisicoes.Should().ContainSingle().Subject;
            requisicao.Metodo.Should().Be(HttpMethod.Post);
            requisicao.Url.Should().Be("https://projeto.supabase.co/storage/v1/object/produtos/produtos/7/foto.webp");
            requisicao.Cabecalhos["apikey"].Should().Be(CHAVE);
            requisicao.Cabecalhos["Authorization"].Should().Be($"Bearer {CHAVE}");
            requisicao.Cabecalhos["x-upsert"].Should().Be("false");
            requisicao.Cabecalhos["cache-control"].Should().Be("max-age=31536000");
            requisicao.ContentType.Should().Be("image/webp");
            requisicao.Corpo.Should().Be("abc");
        }

        [Fact]
        public async Task Should_ApagarNoSupabase_ComListaDeCaminhos()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var http = new HttpFalso(HttpStatusCode.OK);
            var armazenamento = new ArmazenamentoSupabase(new HttpClient(http), ConfigSupabase());

            // Act
            await armazenamento.Apagar(["produtos/7/a.webp", "produtos/7/a-mini.webp"], ct);

            // Assert
            var requisicao = http.Requisicoes.Should().ContainSingle().Subject;
            requisicao.Metodo.Should().Be(HttpMethod.Delete);
            requisicao.Url.Should().Be("https://projeto.supabase.co/storage/v1/object/produtos");

            var corpo = JsonDocument.Parse(requisicao.Corpo).RootElement;
            corpo.GetProperty("prefixes").EnumerateArray().Select(p => p.GetString())
                .Should().Equal("produtos/7/a.webp", "produtos/7/a-mini.webp");
        }

        [Fact]
        public async Task Should_NaoChamarSupabase_When_NadaParaApagar()
        {
            // Arrange
            var http = new HttpFalso(HttpStatusCode.OK);
            var armazenamento = new ArmazenamentoSupabase(new HttpClient(http), ConfigSupabase());

            // Act
            await armazenamento.Apagar([], TestContext.Current.CancellationToken);

            // Assert
            http.Requisicoes.Should().BeEmpty();
        }

        [Fact]
        public async Task Should_LancarErroSemExporAChave_When_SupabaseRecusa()
        {
            // Arrange
            var http = new HttpFalso(HttpStatusCode.Forbidden, """{"error":"Unauthorized"}""");
            var armazenamento = new ArmazenamentoSupabase(new HttpClient(http), ConfigSupabase());

            // Act
            var acao = () => armazenamento.Salvar("foto.webp", Conteudo("x"), "image/webp", TestContext.Current.CancellationToken);

            // Assert
            var erro = (await acao.Should().ThrowAsync<InvalidOperationException>()).Which;
            erro.Message.Should().Contain("403").And.Contain("foto.webp").And.NotContain(CHAVE);
        }

        [Fact]
        public void Should_MontarUrlPublicaDoBucket()
        {
            // Arrange
            var armazenamento = new ArmazenamentoSupabase(new HttpClient(new HttpFalso(HttpStatusCode.OK)), ConfigSupabase());

            // Act
            var url = armazenamento.UrlPublica("produtos/7/foto.webp");

            // Assert
            url.Should().Be("https://projeto.supabase.co/storage/v1/object/public/produtos/produtos/7/foto.webp");
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        private static string NovaPasta() =>
            Path.Combine(Path.GetTempPath(), "belissima-imagens-testes", Guid.NewGuid().ToString("N"));

        private static MemoryStream Conteudo(string texto) => new(Encoding.UTF8.GetBytes(texto));

        private static ArmazenamentoConfig ConfigSupabase() => new()
        {
            Provedor = ArmazenamentoConfig.ProvedorSupabase,
            SupabaseUrl = "https://projeto.supabase.co/",
            Bucket = "produtos",
            ChaveServico = CHAVE
        };

        private record RequisicaoGravada(HttpMethod Metodo, string Url, Dictionary<string, string> Cabecalhos, string? ContentType, string Corpo);

        // Responde sempre o mesmo status e guarda o que recebeu (lido na hora: o conteúdo é descartado depois)
        private sealed class HttpFalso : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _corpo;

            public List<RequisicaoGravada> Requisicoes { get; } = [];

            public HttpFalso(HttpStatusCode status, string corpo = "{}")
            {
                _status = status;
                _corpo = corpo;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var cabecalhos = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
                var corpo = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

                Requisicoes.Add(new RequisicaoGravada(request.Method, request.RequestUri!.ToString(), cabecalhos,
                    request.Content?.Headers.ContentType?.MediaType, corpo));

                return new HttpResponseMessage(_status) { Content = new StringContent(_corpo) };
            }
        }
    }
}
