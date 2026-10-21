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
    public class AdminProdutoOpcaoTests : BaseTesteIntegracao
    {
        public AdminProdutoOpcaoTests(EcommerceFixture fixture) : base(fixture) { }

        private static string Endpoint(int produtoId) => $"/api/admin/produto/{produtoId}/opcao";

        // ════════════════════════════════════════════════════════════════
        // AUTORIZAÇÃO
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return403_When_PerfilCliente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var produto = await PrepararProduto(ct);

            // Act
            var response = await client.PostAsJsonAsync(Endpoint(produto.Id), NovoRequest("Tamanho", "M"), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════
        // REGISTRAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return201_When_OpcaoValida()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct);

            // Act
            var response = await client.PostAsJsonAsync(Endpoint(produto.Id), NovoRequest("personalizacao", "Bordado", 15m), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseProdutoOpcaoAdmin>(EcommerceFixture.JsonOptions, ct);
            body!.Tipo.Should().Be(TipoOpcao.Personalizacao, because: "o tipo é aceito sem diferenciar maiúsculas");
            body.Valor.Should().Be("Bordado");
            body.PrecoAdicional.Should().Be(15m);
            body.Ativo.Should().BeTrue();
        }

        [Theory]
        [InlineData("Estampa", "Logo", "Tipo inválido. Use Tamanho, Cor ou Personalizacao.")]
        [InlineData("Tamanho", "", "O valor da opção não pode ser vazio.")]
        public async Task Should_Return400_When_OpcaoInvalida(string tipo, string valor, string mensagemEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct);

            // Act
            var response = await client.PostAsJsonAsync(Endpoint(produto.Id), NovoRequest(tipo, valor), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be(mensagemEsperada);
        }

        [Fact]
        public async Task Should_Return400_When_PrecoAdicionalNegativo()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct);

            // Act
            var response = await client.PostAsJsonAsync(Endpoint(produto.Id), NovoRequest("Personalizacao", "Bordado", -1m), ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("O preço adicional não pode ser negativo.");
        }

        [Fact]
        public async Task Should_Return400_When_OpcaoJaCadastrada_IgnorandoMaiusculas()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct, Opcao(TipoOpcao.Tamanho, "M"));

            // Act
            var response = await client.PostAsJsonAsync(Endpoint(produto.Id), NovoRequest("Tamanho", "m"), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Esta opção já está cadastrada neste produto.");
        }

        [Fact]
        public async Task Should_PermitirMesmoValor_When_TipoDiferente()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var produto = await PrepararProduto(ct, Opcao(TipoOpcao.Cor, "Única"));

            // Act
            var response = await client.PostAsJsonAsync(Endpoint(produto.Id), NovoRequest("Tamanho", "Única"), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task Should_Return404_When_ProdutoNaoExiste()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);

            // Act
            var response = await client.PostAsJsonAsync(Endpoint(999999), NovoRequest("Tamanho", "M"), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════
        // ATUALIZAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return204AndAtualizar_When_OpcaoValida()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var bordado = Opcao(TipoOpcao.Personalizacao, "Bordado", 15m);
            var produto = await PrepararProduto(ct, bordado);

            var request = new RequestUpdateOpcao { Valor = "Bordado grande", PrecoAdicional = 20m };

            // Act
            var response = await client.PutAsJsonAsync($"{Endpoint(produto.Id)}/{bordado.Id}", request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = _fixture.CriarScope();
            var salva = await _fixture.GetDbContext(scope).ProdutoOpcoes.SingleAsync(ct);

            salva.Valor.Should().Be("Bordado grande");
            salva.PrecoAdicional.Should().Be(20m);
            salva.Tipo.Should().Be(TipoOpcao.Personalizacao);
        }

        [Fact]
        public async Task Should_Return400_When_AtualizarParaValorDeOutraOpcao()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var m = Opcao(TipoOpcao.Tamanho, "M");
            var produto = await PrepararProduto(ct, m, Opcao(TipoOpcao.Tamanho, "G"));

            // Act
            var response = await client.PutAsJsonAsync($"{Endpoint(produto.Id)}/{m.Id}", new RequestUpdateOpcao { Valor = "G" }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Should_Return204_When_AtualizarMantendoOMesmoValor()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var g = Opcao(TipoOpcao.Tamanho, "G");
            var produto = await PrepararProduto(ct, g);

            // Act
            var response = await client.PutAsJsonAsync($"{Endpoint(produto.Id)}/{g.Id}", new RequestUpdateOpcao { Valor = "G", PrecoAdicional = 5m }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent, because: "a própria opção não conta como duplicada");
        }

        // ════════════════════════════════════════════════════════════════
        // REMOVER
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_Return204AndApagar_When_OpcaoSemUso()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var bolso = Opcao(TipoOpcao.Personalizacao, "Bolso extra");
            var produto = await PrepararProduto(ct, bolso);

            // Act
            var response = await client.DeleteAsync($"{Endpoint(produto.Id)}/{bolso.Id}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).ProdutoOpcoes.AnyAsync(ct)).Should().BeFalse();
        }

        [Fact]
        public async Task Should_Return409_When_OpcaoUsadaEmVariante()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var m = Opcao(TipoOpcao.Tamanho, "M");
            var produto = await PrepararProduto(ct, m);

            using (var scope = _fixture.CriarScope())
            {
                var dbContext = _fixture.GetDbContext(scope);
                dbContext.ProdutoVariantes.Add(new ProdutoVariantes { ProdutoId = produto.Id, TamanhoOpcaoId = m.Id });
                await dbContext.SaveChangesAsync(ct);
            }

            // Act
            var response = await client.DeleteAsync($"{Endpoint(produto.Id)}/{m.Id}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Esta opção já está em uso em variantes ou pedidos e não pode ser removida. Desative-a.");
        }

        [Fact]
        public async Task Should_Return404_When_OpcaoDeOutroProduto()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var m = Opcao(TipoOpcao.Tamanho, "M");
            await PrepararProduto(ct, m);
            var outroProduto = await PrepararProduto(ct);

            // Act
            var response = await client.DeleteAsync($"{Endpoint(outroProduto.Id)}/{m.Id}", ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            body.GetProperty("errors")[0].GetString().Should().Be("Opção não encontrada.");
        }

        // ════════════════════════════════════════════════════════════════
        // ATIVAR E DESATIVAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Should_SumirDaVitrine_When_OpcaoDesativada()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var bordado = Opcao(TipoOpcao.Personalizacao, "Bordado");
            var produto = await PrepararProduto(ct, bordado);

            // Act
            var response = await client.PatchAsJsonAsync($"{Endpoint(produto.Id)}/{bordado.Id}/status", new RequestAlterarStatus { Ativo = false }, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var vitrine = await _fixture.Client.GetFromJsonAsync<ResponseProdutoDetalhe>($"/api/produto/{produto.Id}", ct);
            vitrine!.Personalizacoes.Should().BeEmpty();
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

        private static ProdutoOpcoes Opcao(TipoOpcao tipo, string valor, decimal precoAdicional = 0) => new()
        {
            Tipo = tipo,
            Valor = valor,
            PrecoAdicional = precoAdicional
        };

        private static RequestRegistrarOpcao NovoRequest(string tipo, string valor, decimal precoAdicional = 0) => new()
        {
            Tipo = tipo,
            Valor = valor,
            PrecoAdicional = precoAdicional
        };
    }
}
