using API.IntegrationTests.Fixture;
using Application.Request;
using Application.Response;
using Domain.Entities;
using Domain.Enums;
using Domain.Security.Perfis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    public class AdminProdutoVarianteTests : BaseTesteIntegracao
    {
        public AdminProdutoVarianteTests(EcommerceFixture fixture) : base(fixture) { }

        private static string Endpoint(int produtoId) => $"/api/admin/produto/{produtoId}/variante";

        // ════════════════════════════════════════════════════════════════
        // AUTORIZAÇÃO
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return403_When_PerfilCliente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var produto = await PrepararProduto(ct, Opcao(TipoOpcao.Tamanho, "M"));

            // Act
            var response = await client.PostAsync($"{Endpoint(produto.Id)}/gerar", null, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════
        // GERAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_GerarTamanhosVezesCores_ComEstoqueZerado()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct,
                Opcao(TipoOpcao.Tamanho, "P"),
                Opcao(TipoOpcao.Tamanho, "M"),
                Opcao(TipoOpcao.Cor, "Azul"),
                Opcao(TipoOpcao.Cor, "Branco"),
                Opcao(TipoOpcao.Personalizacao, "Bordado"));

            // Act
            var response = await client.PostAsync($"{Endpoint(produto.Id)}/gerar", null, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseVariantesGeradas>(ct);
            body!.VariantesCriadas.Select(v => $"{v.Tamanho}/{v.Cor}")
                .Should().BeEquivalentTo("P/Azul", "P/Branco", "M/Azul", "M/Branco");
            body.VariantesCriadas.Should().OnlyContain(v => v.Ativo && v.Quantidade == 0);

            using var scope = _fixture.CriarScope();
            var estoques = await _fixture.GetDbContext(scope).Estoque.ToListAsync(ct);
            estoques.Should().HaveCount(4).And.OnlyContain(e => e.Quantidade == 0);
        }

        [Fact]
        public async Task Should_GerarUmaVariantePorTamanho_When_ProdutoSemCor()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct, Opcao(TipoOpcao.Tamanho, "P"), Opcao(TipoOpcao.Tamanho, "M"));

            // Act
            var response = await client.PostAsync($"{Endpoint(produto.Id)}/gerar", null, ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseVariantesGeradas>(ct);
            body!.VariantesCriadas.Should().HaveCount(2).And.OnlyContain(v => v.CorId == null && v.Cor == null);
        }

        [Fact]
        public async Task Should_CriarSoOQueFalta_When_GeradoNovamente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct, Opcao(TipoOpcao.Tamanho, "M"), Opcao(TipoOpcao.Cor, "Azul"));

            await client.PostAsync($"{Endpoint(produto.Id)}/gerar", null, ct);

            var novoTamanho = new RequestRegistrarOpcao { Tipo = "Tamanho", Valor = "G" };
            await client.PostAsJsonAsync($"/api/admin/produto/{produto.Id}/opcao", novoTamanho, ct);

            // Act
            var response = await client.PostAsync($"{Endpoint(produto.Id)}/gerar", null, ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseVariantesGeradas>(ct);
            body!.VariantesCriadas.Should().ContainSingle(v => v.Tamanho == "G" && v.Cor == "Azul");

            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).ProdutoVariantes.CountAsync(ct)).Should().Be(2);
        }

        [Fact]
        public async Task Should_IgnorarOpcoesInativas()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct,
                Opcao(TipoOpcao.Tamanho, "M"),
                Opcao(TipoOpcao.Tamanho, "GG", ativo: false),
                Opcao(TipoOpcao.Cor, "Azul"),
                Opcao(TipoOpcao.Cor, "Verde", ativo: false));

            // Act
            var response = await client.PostAsync($"{Endpoint(produto.Id)}/gerar", null, ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponseVariantesGeradas>(ct);
            body!.VariantesCriadas.Should().ContainSingle(v => v.Tamanho == "M" && v.Cor == "Azul");
        }

        [Fact]
        public async Task Should_Return400_When_SemTamanhoAtivo()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct, Opcao(TipoOpcao.Cor, "Azul"), Opcao(TipoOpcao.Tamanho, "M", ativo: false));

            // Act
            var response = await client.PostAsync($"{Endpoint(produto.Id)}/gerar", null, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Cadastre ao menos um tamanho ativo antes de gerar as variantes.");
        }

        [Fact]
        public async Task Should_Return404_When_GerarParaProdutoInexistente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.PostAsync($"{Endpoint(999999)}/gerar", null, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════
        // ATIVAR E DESATIVAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_SumirDaVitrine_When_VarianteDesativada()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct, Opcao(TipoOpcao.Tamanho, "M"));

            var gerado = await client.PostAsync($"{Endpoint(produto.Id)}/gerar", null, ct);
            var variante = (await gerado.Content.ReadFromJsonAsync<ResponseVariantesGeradas>(ct))!.VariantesCriadas.Single();

            // Act
            var response = await client.PatchAsJsonAsync($"{Endpoint(produto.Id)}/{variante.Id}/status", new RequestAlterarStatus { Ativo = false }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var vitrine = await _fixture.Client.GetFromJsonAsync<ResponseProdutoDetalhe>($"/api/produto/{produto.Id}", ct);
            vitrine!.Variantes.Should().BeEmpty();
        }

        [Fact]
        public async Task Should_Return404_When_VarianteDeOutroProduto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct, Opcao(TipoOpcao.Tamanho, "M"));
            var outroProduto = await PrepararProduto(ct);

            var gerado = await client.PostAsync($"{Endpoint(produto.Id)}/gerar", null, ct);
            var variante = (await gerado.Content.ReadFromJsonAsync<ResponseVariantesGeradas>(ct))!.VariantesCriadas.Single();

            // Act
            var response = await client.PatchAsJsonAsync($"{Endpoint(outroProduto.Id)}/{variante.Id}/status", new RequestAlterarStatus { Ativo = false }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Variante não encontrada.");
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        private async Task<Produtos> PrepararProduto(CancellationToken ct, params ProdutoOpcoes[] opcoes)
        {
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            var produto = new Produtos { Nome = "Camiseta Polo", PrecoBase = 59.90m, Ativo = true };

            foreach (var opcao in opcoes)
                produto.Opcoes.Add(opcao);

            await dbContext.Produtos.AddAsync(produto, ct);
            await dbContext.SaveChangesAsync(ct);

            return produto;
        }

        private static ProdutoOpcoes Opcao(TipoOpcao tipo, string valor, bool ativo = true) => new()
        {
            Tipo = tipo,
            Valor = valor,
            Ativo = ativo
        };
    }
}
