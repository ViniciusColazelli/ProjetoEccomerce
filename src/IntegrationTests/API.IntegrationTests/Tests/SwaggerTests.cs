using API.IntegrationTests.Fixture;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace API.IntegrationTests.Tests
{
    // O Swagger só é servido em Development, mas o documento é gerado pelo mesmo serviço:
    // gerar aqui pega rota sem documentação e schema que o Swashbuckle não consegue montar
    public class SwaggerTests : BaseTesteIntegracao
    {
        public SwaggerTests(EcommerceFixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("/api/pedidos", "GET", "200,400,401")]
        [InlineData("/api/pedidos", "POST", "201,400,401,409")]
        [InlineData("/api/pedidos/{id}", "GET", "200,401,404")]
        [InlineData("/api/pedidos/{id}/cancelar", "POST", "200,401,404,409")]
        [InlineData("/api/admin/pedidos", "GET", "200,400,401,403")]
        [InlineData("/api/admin/pedidos/{id}", "GET", "200,401,403,404")]
        [InlineData("/api/admin/pedidos/{id}/status", "POST", "200,400,401,403,404,409")]
        public void Should_DocumentarRotasDePedido(string rota, string metodo, string respostas)
        {
            // Arrange
            using var scope = _fixture.CriarScope();
            var swagger = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

            // Act
            var documento = swagger.GetSwagger("v1");

            // Assert
            documento.Paths.Should().ContainKey(rota);
            var operacao = documento.Paths[rota].Operations.Should().ContainKey(new HttpMethod(metodo)).WhoseValue;
            operacao.Responses.Should().NotBeNull();
            operacao.Responses!.Keys.Should().BeEquivalentTo(respostas.Split(','));
        }

        [Theory]
        [InlineData("/api/admin/produto/{produtoId}/imagem", "POST", "201,400,401,403,404")]
        [InlineData("/api/admin/produto/{produtoId}/imagem/{imagemId}", "DELETE", "204,401,403,404")]
        [InlineData("/api/admin/produto/{produtoId}/imagem/ordem", "PUT", "200,400,401,403,404")]
        public void Should_DocumentarRotasDeImagens(string rota, string metodo, string respostas)
        {
            // Arrange
            using var scope = _fixture.CriarScope();
            var swagger = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

            // Act
            var documento = swagger.GetSwagger("v1");

            // Assert
            documento.Paths.Should().ContainKey(rota);
            var operacao = documento.Paths[rota].Operations.Should().ContainKey(new HttpMethod(metodo)).WhoseValue;
            operacao.Responses!.Keys.Should().BeEquivalentTo(respostas.Split(','));
        }

        [Theory]
        [InlineData("/api/Login", "POST", "200,401,429")]
        [InlineData("/api/Login/refresh", "POST", "204,401,429")]
        [InlineData("/api/Login/logout", "POST", "204")]
        public void Should_DocumentarRotasDeSessao(string rota, string metodo, string respostas)
        {
            // Arrange
            using var scope = _fixture.CriarScope();
            var swagger = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

            // Act
            var documento = swagger.GetSwagger("v1");

            // Assert
            documento.Paths.Should().ContainKey(rota);
            var operacao = documento.Paths[rota].Operations.Should().ContainKey(new HttpMethod(metodo)).WhoseValue;
            operacao.Responses!.Keys.Should().BeEquivalentTo(respostas.Split(','));
        }

        // O envio aparece no Swagger como formulário com os dois arquivos, e dá para testar por lá
        [Fact]
        public void Should_DocumentarEnvioDeFotoComoMultipart()
        {
            // Arrange
            using var scope = _fixture.CriarScope();
            var swagger = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

            // Act
            var documento = swagger.GetSwagger("v1");

            // Assert
            var operacao = documento.Paths["/api/admin/produto/{produtoId}/imagem"].Operations![HttpMethod.Post];
            var formulario = operacao.RequestBody!.Content.Should().ContainKey("multipart/form-data").WhoseValue;
            formulario.Schema!.Properties.Should().ContainKeys("imagem", "miniatura");
        }

        // O cadeado do Swagger: o esquema JWT existe e é exigido em todas as rotas
        [Fact]
        public void Should_DocumentarAutenticacaoJwt()
        {
            // Arrange
            using var scope = _fixture.CriarScope();
            var swagger = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

            // Act
            var documento = swagger.GetSwagger("v1");

            // Assert
            var esquema = documento.Components!.SecuritySchemes.Should().ContainKey("Bearer").WhoseValue;
            esquema.Type.Should().Be(SecuritySchemeType.Http);
            esquema.Scheme.Should().Be("Bearer");
            esquema.BearerFormat.Should().Be("JWT");

            documento.Security.Should().ContainSingle()
                .Which.Keys.Should().ContainSingle(referencia => referencia.Reference.Id == "Bearer");
        }

        [Theory]
        [InlineData("ResponsePedido")]
        [InlineData("ResponsePedidoAdmin")]
        [InlineData("ResponsePedidos")]
        [InlineData("ResponsePedidosAdmin")]
        [InlineData("RequestCriarPedido")]
        [InlineData("RequestAlterarStatusPedido")]
        public void Should_GerarSchemasDosPedidos(string schema)
        {
            // Arrange
            using var scope = _fixture.CriarScope();
            var swagger = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

            // Act
            var documento = swagger.GetSwagger("v1");

            // Assert
            documento.Components!.Schemas.Should().ContainKey(schema);
        }
    }
}
