using API.IntegrationTests.Fixture;
using Application.Request;
using Application.UseCases.EstoqueVariante.Saida;
using Domain.Entities;
using Domain.Enums;
using Domain.Security.Perfis;
using Exceptions.ExceptionBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace API.IntegrationTests.Tests
{
    // Garante que o saldo nunca fica errado quando duas operações mexem na mesma variante ao mesmo tempo.
    // É a base para os pedidos: duas compras disputando a última peça.
    public class EstoqueConcorrenciaTests : BaseTesteIntegracao
    {
        public EstoqueConcorrenciaTests(EcommerceFixture fixture) : base(fixture) { }

        private const string Endpoint = "/api/admin/estoque";
        private const int REQUISICOES_SIMULTANEAS = 5;

        // ════════════════════════════════════════════════════════════════
        // CONTROLE OTIMISTA (xmin)
        // ════════════════════════════════════════════════════════════════

        // Cenário montado passo a passo, sem depender de timing: a operação lê o saldo,
        // outra operação altera a linha, e só então a primeira tenta salvar
        [Fact]
        public async Task Should_LancarConflitoSemAlterarSaldo_When_SaldoAlteradoPorOutraOperacao()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            var variante = await PrepararVariante(ct, quantidade: 10);

            using var scopeOperacao = _fixture.CriarScope();
            var useCase = scopeOperacao.ServiceProvider.GetRequiredService<ISaidaEstoqueUseCase>();

            // a operação "lê" o saldo (10): o EF passa a rastrear a linha com o xmin desse momento
            await _fixture.GetDbContext(scopeOperacao).Estoque.SingleAsync(e => e.VarianteId == variante.Id, ct);

            // outra operação altera o saldo antes dela salvar
            using (var scopeOutra = _fixture.CriarScope())
            {
                await _fixture.GetDbContext(scopeOutra).Estoque
                    .Where(e => e.VarianteId == variante.Id)
                    .ExecuteUpdateAsync(set => set.SetProperty(e => e.Quantidade, 3), ct);
            }

            // Act
            var acao = () => useCase.Execute(variante.Id, new RequestMovimentarEstoque { Quantidade = 5, Motivo = "Venda balcão" });

            // Assert
            await acao.Should().ThrowAsync<ConflitoException>()
                .WithMessage("Este registro foi alterado por outra operação. Tente novamente.");

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            (await dbContext.Estoque.SingleAsync(ct)).Quantidade.Should().Be(3);
            (await dbContext.EstoqueMovimentacoes.CountAsync(ct)).Should().Be(0);
        }

        // ════════════════════════════════════════════════════════════════
        // REQUISIÇÕES SIMULTÂNEAS
        // ════════════════════════════════════════════════════════════════

        // Independe da ordem em que o servidor atende: só uma saída cabe no saldo,
        // as outras levam 409 (estoque insuficiente ou registro alterado)
        [Fact]
        public async Task Should_LiberarSoUmaSaida_When_SaidasSimultaneasDaUltimaPeca()
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, quantidade: 1);

            // Act
            var status = await DispararSimultaneas(client, $"{Endpoint}/{variante.Id}/saida", ct);

            // Assert
            status.Count(s => s == HttpStatusCode.OK).Should().Be(1);
            status.Count(s => s == HttpStatusCode.Conflict).Should().Be(REQUISICOES_SIMULTANEAS - 1);

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            (await dbContext.Estoque.SingleAsync(ct)).Quantidade.Should().Be(0);
            (await dbContext.EstoqueMovimentacoes.CountAsync(ct)).Should().Be(1);
        }

        // Nenhuma entrada aceita pode se perder: o saldo final bate com as respostas 200
        [Theory]
        [InlineData(0)]
        [InlineData(null)]   // variante antiga, sem linha de estoque: as entradas disputam o INSERT
        public async Task Should_NaoPerderEntrada_When_EntradasSimultaneas(int? saldoInicial)
        {
            // Arrange
            var ct = TestContext.Current.CancellationToken;
            using var client = await _fixture.CriarClientAutenticado(Perfis.Admin);
            var variante = await PrepararVariante(ct, quantidade: saldoInicial);

            // Act
            var status = await DispararSimultaneas(client, $"{Endpoint}/{variante.Id}/entrada", ct);

            // Assert
            status.Should().OnlyContain(s => s == HttpStatusCode.OK || s == HttpStatusCode.Conflict);

            var aceitas = status.Count(s => s == HttpStatusCode.OK);
            aceitas.Should().BeGreaterThan(0);

            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);
            (await dbContext.Estoque.SingleAsync(ct)).Quantidade.Should().Be(aceitas);
            (await dbContext.EstoqueMovimentacoes.CountAsync(ct)).Should().Be(aceitas);
        }

        // ── Helpers ───────────────────────────────────────────────────

        // Cada requisição movimenta 1 unidade
        private static async Task<HttpStatusCode[]> DispararSimultaneas(HttpClient client, string url, CancellationToken ct)
        {
            var request = new RequestMovimentarEstoque { Quantidade = 1, Motivo = "Teste de concorrência" };

            var respostas = await Task.WhenAll(Enumerable.Range(0, REQUISICOES_SIMULTANEAS)
                .Select(_ => client.PostAsJsonAsync(url, request, ct)));

            return respostas.Select(resposta => resposta.StatusCode).ToArray();
        }

        private async Task<ProdutoVariantes> PrepararVariante(CancellationToken ct, int? quantidade)
        {
            using var scope = _fixture.CriarScope();
            var dbContext = _fixture.GetDbContext(scope);

            var produto = new Produtos { Nome = "Jaleco", PrecoBase = 59.90m, Ativo = true };

            var opcaoTamanho = new ProdutoOpcoes { Tipo = TipoOpcao.Tamanho, Valor = "M", Ativo = true };
            produto.Opcoes.Add(opcaoTamanho);

            var variante = new ProdutoVariantes
            {
                TamanhoOpcao = opcaoTamanho,
                Ativo = true,
                Estoque = quantidade.HasValue ? new Estoque { Quantidade = quantidade.Value } : null
            };
            produto.Variantes.Add(variante);

            await dbContext.Produtos.AddAsync(produto, ct);
            await dbContext.SaveChangesAsync(ct);

            return variante;
        }
    }
}
