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
    public class CriarPedidoTests : BaseTesteIntegracao
    {
        public CriarPedidoTests(EcommerceFixture fixture) : base(fixture) { }

        private const string Endpoint = "/api/pedidos";

        // ════════════════════════════════════════════════════════════════
        // FLUXO FELIZ
        // ════════════════════════════════════════════════════════════════

        // 2 × M/Azul com Bordado (+15) e Nome (+10) = 2 × 84,90 = 169,80
        // 1 × M/Azul sem personalização          = 1 × 59,90 =  59,90
        //                                           total    = 229,70
        [Fact]
        public async Task Should_Return201ComPrecosCalculadosNoServidor()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque: 10);

            var request = new RequestCriarPedido
            {
                Itens =
                {
                    new RequestItemPedido { VarianteId = catalogo.MAzul.Id, Quantidade = 2, PersonalizacaoIds = { catalogo.Bordado.Id, catalogo.Nome.Id } },
                    new RequestItemPedido { VarianteId = catalogo.MAzul.Id, Quantidade = 1 }
                },
                Observacao = "  Entregar na recepção  "
            };

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, request, EcommerceFixture.JsonOptions, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = (await response.Content.ReadFromJsonAsync<ResponsePedido>(EcommerceFixture.JsonOptions, ct))!;
            response.Headers.Location!.ToString().Should().Be($"/api/pedidos/{body.Id}");

            body.Status.Should().Be(StatusPedido.AguardandoPagamento);
            body.Total.Should().Be(229.70m);
            body.Observacao.Should().Be("Entregar na recepção");
            body.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
            body.Itens.Should().HaveCount(2);

            var personalizado = body.Itens[0];
            personalizado.Produto.Should().Be("Camiseta Polo");
            personalizado.Tamanho.Should().Be("M");
            personalizado.Cor.Should().Be("Azul");
            personalizado.Quantidade.Should().Be(2);
            personalizado.PrecoUnitario.Should().Be(84.90m);
            personalizado.Subtotal.Should().Be(169.80m);
            personalizado.Personalizacoes.Select(p => (p.Valor, p.PrecoAdicional))
                .Should().Equal(("Bordado", 15m), ("Nome", 10m));

            var simples = body.Itens[1];
            simples.PrecoUnitario.Should().Be(59.90m);
            simples.Subtotal.Should().Be(59.90m);
            simples.Personalizacoes.Should().BeEmpty();

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            var pedido = await dbContext.Pedidos.Include(p => p.Itens).ThenInclude(i => i.Customizacoes).SingleAsync(ct);
            pedido.ClienteId.Should().Be(await dbContext.Clientes.Select(c => c.Id).SingleAsync(ct));
            pedido.Total.Should().Be(229.70m);
            pedido.Itens.SelectMany(i => i.Customizacoes).Should().HaveCount(2);
        }

        // O pagamento é manual: o estoque só baixa quando o admin confirmar
        [Fact]
        public async Task Should_NaoBaixarEstoque_When_PedidoCriado()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque: 10);

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, UmItem(catalogo.MAzul.Id, quantidade: 10), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            (await dbContext.Estoque.SingleAsync(e => e.VarianteId == catalogo.MAzul.Id, ct)).Quantidade.Should().Be(10);
            (await dbContext.EstoqueMovimentacoes.CountAsync(ct)).Should().Be(0);
        }

        // Campos de preço que o front mandar são ignorados: o servidor usa o cadastro
        [Fact]
        public async Task Should_IgnorarPrecosEnviadosPeloFront()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque: 10);

            var request = new
            {
                total = 0.01m,
                itens = new[] { new { varianteId = catalogo.MAzul.Id, quantidade = 1, precoUnitario = 0.01m, personalizacaoIds = Array.Empty<int>() } }
            };

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, request, ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponsePedido>(EcommerceFixture.JsonOptions, ct);
            body!.Total.Should().Be(59.90m);
        }

        [Fact]
        public async Task Should_GuardarObservacaoNula_When_SoEspacos()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque: 10);

            var request = UmItem(catalogo.MAzul.Id);
            request.Observacao = "   ";

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, request, ct);

            // Assert
            var body = await response.Content.ReadFromJsonAsync<ResponsePedido>(EcommerceFixture.JsonOptions, ct);
            body!.Observacao.Should().BeNull();
        }

        [Fact]
        public async Task Should_Return401_When_SemLogin()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = _fixture.CriarClientSemCookies();

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, new RequestCriarPedido(), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════
        // VALIDAÇÃO DO PEDIDO (400)
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("sem itens", "O pedido precisa ter ao menos um item.")]
        [InlineData("quantidade 0", "A quantidade de cada item deve estar entre 1 e 999.")]
        [InlineData("quantidade 1000", "A quantidade de cada item deve estar entre 1 e 999.")]
        [InlineData("personalização repetida", "A mesma personalização foi escolhida mais de uma vez no item.")]
        [InlineData("observação longa", "A observação deve ter no máximo 500 caracteres.")]
        [InlineData("51 itens", "O pedido pode ter no máximo 50 itens.")]
        public async Task Should_Return400_When_PedidoInvalido(string caso, string mensagemEsperada)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque: 10);

            var request = caso switch
            {
                "sem itens" => new RequestCriarPedido(),
                "quantidade 0" => UmItem(catalogo.MAzul.Id, quantidade: 0),
                "quantidade 1000" => UmItem(catalogo.MAzul.Id, quantidade: 1000),
                "personalização repetida" => UmItem(catalogo.MAzul.Id, personalizacoes: [catalogo.Bordado.Id, catalogo.Bordado.Id]),
                "observação longa" => UmItem(catalogo.MAzul.Id, observacao: new string('a', 501)),
                "51 itens" => new RequestCriarPedido
                {
                    Itens = Enumerable.Range(0, 51).Select(_ => new RequestItemPedido { VarianteId = catalogo.MAzul.Id, Quantidade = 1 }).ToList()
                },
                _ => throw new ArgumentOutOfRangeException(nameof(caso))
            };

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await MensagensDeErro(response, ct)).Should().ContainSingle().Which.Should().Be(mensagemEsperada);
            await DeveNaoTerPedido(ct);
        }

        [Fact]
        public async Task Should_Return400_When_VarianteNaoExiste()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            await PrepararCatalogo(ct, estoque: 10);

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, UmItem(varianteId: 999999), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await MensagensDeErro(response, ct)).Should().Equal("Uma das peças escolhidas não foi encontrada.");
        }

        // Mesma regra da vitrine: o que não aparece para o cliente também não pode ser comprado
        [Theory]
        [InlineData("variante")]
        [InlineData("produto")]
        [InlineData("tamanho")]
        [InlineData("cor")]
        public async Task Should_Return400_When_PecaForaDaVitrine(string desativado)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque: 10);

            using (var scope = _fixture.CriarScope())
            {
                var dbContext = _fixture.GetDbContext(scope);
                _ = desativado switch
                {
                    "variante" => await dbContext.ProdutoVariantes.Where(v => v.Id == catalogo.MAzul.Id).ExecuteUpdateAsync(s => s.SetProperty(v => v.Ativo, false), ct),
                    "produto" => await dbContext.Produtos.ExecuteUpdateAsync(s => s.SetProperty(p => p.Ativo, false), ct),
                    "tamanho" => await dbContext.ProdutoOpcoes.Where(o => o.Valor == "M").ExecuteUpdateAsync(s => s.SetProperty(o => o.Ativo, false), ct),
                    "cor" => await dbContext.ProdutoOpcoes.Where(o => o.Valor == "Azul").ExecuteUpdateAsync(s => s.SetProperty(o => o.Ativo, false), ct),
                    _ => throw new ArgumentOutOfRangeException(nameof(desativado))
                };
            }

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, UmItem(catalogo.MAzul.Id), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await MensagensDeErro(response, ct)).Should().Equal("A peça Camiseta Polo (M, Azul) não está mais disponível.");
            await DeveNaoTerPedido(ct);
        }

        [Theory]
        [InlineData("de outro produto")]
        [InlineData("tipo Cor")]
        [InlineData("inativa")]
        [InlineData("inexistente")]
        public async Task Should_Return400_When_PersonalizacaoInvalida(string caso)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque: 10);

            int personalizacaoId;
            using (var scope = _fixture.CriarScope())
            {
                var dbContext = _fixture.GetDbContext(scope);

                switch (caso)
                {
                    case "de outro produto":
                        var outro = new Produtos { Nome = "Jaleco", PrecoBase = 99m, Ativo = true };
                        var logo = new ProdutoOpcoes { Tipo = TipoOpcao.Personalizacao, Valor = "Logo", PrecoAdicional = 20m };
                        outro.Opcoes.Add(logo);
                        dbContext.Produtos.Add(outro);
                        await dbContext.SaveChangesAsync(ct);
                        personalizacaoId = logo.Id;
                        break;
                    case "tipo Cor":
                        personalizacaoId = catalogo.MAzul.CorOpcaoId!.Value;
                        break;
                    case "inativa":
                        await dbContext.ProdutoOpcoes.Where(o => o.Id == catalogo.Bordado.Id).ExecuteUpdateAsync(s => s.SetProperty(o => o.Ativo, false), ct);
                        personalizacaoId = catalogo.Bordado.Id;
                        break;
                    default:
                        personalizacaoId = 999999;
                        break;
                }
            }

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, UmItem(catalogo.MAzul.Id, personalizacoes: [personalizacaoId]), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await MensagensDeErro(response, ct)).Should().Equal("A personalização escolhida não está disponível para Camiseta Polo.");
            await DeveNaoTerPedido(ct);
        }

        // Um item com problema barra o pedido inteiro: nada é gravado pela metade
        [Fact]
        public async Task Should_NaoGravarNada_When_UmDosItensInvalido()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque: 10);

            var request = new RequestCriarPedido
            {
                Itens =
                {
                    new RequestItemPedido { VarianteId = catalogo.MAzul.Id, Quantidade = 1 },
                    new RequestItemPedido { VarianteId = 999999, Quantidade = 1 }
                }
            };

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await DeveNaoTerPedido(ct);
        }

        // ════════════════════════════════════════════════════════════════
        // ESTOQUE (409)
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(1, 2)]
        [InlineData(0, 1)]
        [InlineData(null, 1)]   // variante antiga, sem linha de estoque
        public async Task Should_Return409_When_EstoqueInsuficiente(int? estoque, int quantidade)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque);

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, UmItem(catalogo.MAzul.Id, quantidade), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await MensagensDeErro(response, ct)).Should().Equal("Não há estoque suficiente de Camiseta Polo (M, Azul).");
            await DeveNaoTerPedido(ct);
        }

        [Fact]
        public async Task Should_Return201_When_PedidoUsaExatamenteOEstoque()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque: 3);

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, UmItem(catalogo.MAzul.Id, quantidade: 3), ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // A mesma peça em dois itens (com e sem bordado) disputa o mesmo estoque
        [Fact]
        public async Task Should_SomarItensDaMesmaVariante_When_ConfereEstoque()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque: 3);

            var request = new RequestCriarPedido
            {
                Itens =
                {
                    new RequestItemPedido { VarianteId = catalogo.MAzul.Id, Quantidade = 2, PersonalizacaoIds = { catalogo.Bordado.Id } },
                    new RequestItemPedido { VarianteId = catalogo.MAzul.Id, Quantidade = 2 }
                }
            };

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Should_ListarTodasAsPecasSemEstoque()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Cliente);
            var catalogo = await PrepararCatalogo(ct, estoque: 0);

            var request = new RequestCriarPedido
            {
                Itens =
                {
                    new RequestItemPedido { VarianteId = catalogo.MAzul.Id, Quantidade = 1 },
                    new RequestItemPedido { VarianteId = catalogo.GAzul.Id, Quantidade = 1 }
                }
            };

            // Act
            var response = await client.PostAsJsonAsync(Endpoint, request, ct);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await MensagensDeErro(response, ct)).Should().BeEquivalentTo(
                "Não há estoque suficiente de Camiseta Polo (M, Azul).",
                "Não há estoque suficiente de Camiseta Polo (G, Azul).");
        }

        // ════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════

        private record Catalogo(ProdutoVariantes MAzul, ProdutoVariantes GAzul, ProdutoOpcoes Bordado, ProdutoOpcoes Nome);

        // Camiseta Polo (59,90) com M e G em Azul, e as personalizações Bordado (+15) e Nome (+10)
        private async Task<Catalogo> PrepararCatalogo(CancellationToken ct, int? estoque)
        {
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            var m = new ProdutoOpcoes { Tipo = TipoOpcao.Tamanho, Valor = "M" };
            var g = new ProdutoOpcoes { Tipo = TipoOpcao.Tamanho, Valor = "G" };
            var azul = new ProdutoOpcoes { Tipo = TipoOpcao.Cor, Valor = "Azul" };
            var bordado = new ProdutoOpcoes { Tipo = TipoOpcao.Personalizacao, Valor = "Bordado", PrecoAdicional = 15m };
            var nome = new ProdutoOpcoes { Tipo = TipoOpcao.Personalizacao, Valor = "Nome", PrecoAdicional = 10m };

            var mAzul = NovaVariante(m, azul, estoque);
            var gAzul = NovaVariante(g, azul, estoque);

            dbContext.Produtos.Add(new Produtos
            {
                Nome = "Camiseta Polo",
                PrecoBase = 59.90m,
                Ativo = true,
                Opcoes = { m, g, azul, bordado, nome },
                Variantes = { mAzul, gAzul }
            });

            await dbContext.SaveChangesAsync(ct);

            return new Catalogo(mAzul, gAzul, bordado, nome);
        }

        private static ProdutoVariantes NovaVariante(ProdutoOpcoes tamanho, ProdutoOpcoes cor, int? estoque) => new()
        {
            TamanhoOpcao = tamanho,
            CorOpcao = cor,
            Ativo = true,
            Estoque = estoque.HasValue ? new Estoque { Quantidade = estoque.Value } : null
        };

        private static RequestCriarPedido UmItem(int varianteId, int quantidade = 1, int[]? personalizacoes = null, string? observacao = null) => new()
        {
            Itens = { new RequestItemPedido { VarianteId = varianteId, Quantidade = quantidade, PersonalizacaoIds = personalizacoes?.ToList() ?? new() } },
            Observacao = observacao
        };

        private async Task DeveNaoTerPedido(CancellationToken ct)
        {
            using var scope = _fixture.CriarScope();
            (await _fixture.GetDbContext(scope).Pedidos.CountAsync(ct)).Should().Be(0);
        }

        private static async Task<List<string?>> MensagensDeErro(HttpResponseMessage response, CancellationToken ct)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            return body.GetProperty("errors").EnumerateArray().Select(erro => erro.GetString()).ToList();
        }
    }
}
