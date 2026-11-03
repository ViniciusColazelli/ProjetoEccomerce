using API.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace API.IntegrationTests.Tests
{
    // Os cenários reproduzem os cabeçalhos observados em produção (Vercel → Cloudflare → Render).
    // Não usa banco nem API: testa só a regra que escolhe o IP para o rate limit.
    public class IpClienteTests
    {
        private const string SEGREDO = "YS3FORaRHy7YUU6YG4p3PqVcKDlA2poF";

        private const string CLIENTE = "179.215.132.236";
        private const string SERVIDOR_VERCEL = "56.125.194.93";
        private const string BORDA_CLOUDFLARE = "104.23.243.64";
        private const string FALSO = "1.2.3.4";

        [Fact]
        public void Should_UsarCfConnectingIp_When_RequisicaoDiretaNoRender()
        {
            // Arrange
            var context = CriarContext(SEGREDO, cabecalhos: new()
            {
                ["X-Forwarded-For"] = $"{CLIENTE},{BORDA_CLOUDFLARE}",
                ["CF-Connecting-IP"] = CLIENTE
            });

            // Act
            var ip = context.ObterIpCliente();

            // Assert
            ip.Should().Be(CLIENTE);
        }

        [Fact]
        public void Should_IgnorarXForwardedForFalso_When_RequisicaoDiretaSemSegredo()
        {
            // Arrange: o atacante manda o X-Forwarded-For falso direto no Render, e o Cloudflare só acrescenta
            var context = CriarContext(SEGREDO, cabecalhos: new()
            {
                ["X-Forwarded-For"] = $"{FALSO},{CLIENTE},{BORDA_CLOUDFLARE}",
                ["CF-Connecting-IP"] = CLIENTE
            });

            // Act
            var ip = context.ObterIpCliente();

            // Assert
            ip.Should().Be(CLIENTE);
        }

        [Fact]
        public void Should_UsarPrimeiroXForwardedFor_When_VeioPelaVercelComSegredo()
        {
            // Arrange
            var context = CriarContext(SEGREDO, cabecalhos: new()
            {
                ["X-Forwarded-For"] = $"{CLIENTE},{SERVIDOR_VERCEL},{BORDA_CLOUDFLARE}",
                ["CF-Connecting-IP"] = SERVIDOR_VERCEL,
                [IpClienteExtension.CabecalhoSegredoProxy] = SEGREDO
            });

            // Act
            var ip = context.ObterIpCliente();

            // Assert
            ip.Should().Be(CLIENTE, because: "o IP da Vercel é compartilhado por muitos usuários");
        }

        [Theory]
        [InlineData("segredo-errado", SEGREDO)]
        [InlineData(SEGREDO, null)]                  // segredo não configurado na API: nunca confia
        [InlineData(SEGREDO, "")]
        [InlineData("", SEGREDO)]
        public void Should_IgnorarXForwardedFor_When_SegredoNaoConfere(string segredoRecebido, string? segredoConfigurado)
        {
            // Arrange
            var context = CriarContext(segredoConfigurado, cabecalhos: new()
            {
                ["X-Forwarded-For"] = $"{FALSO},{CLIENTE},{BORDA_CLOUDFLARE}",
                ["CF-Connecting-IP"] = CLIENTE,
                [IpClienteExtension.CabecalhoSegredoProxy] = segredoRecebido
            });

            // Act
            var ip = context.ObterIpCliente();

            // Assert
            ip.Should().Be(CLIENTE);
        }

        [Fact]
        public void Should_IgnorarXVercelForwardedFor()
        {
            // Arrange: em produção a Vercel repassa esse cabeçalho como veio do cliente
            var context = CriarContext(SEGREDO, cabecalhos: new()
            {
                ["X-Vercel-Forwarded-For"] = FALSO,
                ["CF-Connecting-IP"] = CLIENTE
            });

            // Act
            var ip = context.ObterIpCliente();

            // Assert
            ip.Should().Be(CLIENTE);
        }

        [Fact]
        public void Should_UsarCfConnectingIp_When_XForwardedForInvalidoMesmoComSegredo()
        {
            // Arrange
            var context = CriarContext(SEGREDO, cabecalhos: new()
            {
                ["X-Forwarded-For"] = "nao-e-ip",
                ["CF-Connecting-IP"] = SERVIDOR_VERCEL,
                [IpClienteExtension.CabecalhoSegredoProxy] = SEGREDO
            });

            // Act
            var ip = context.ObterIpCliente();

            // Assert
            ip.Should().Be(SERVIDOR_VERCEL);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("nao-e-ip")]
        public void Should_UsarIpDaConexao_When_SemCloudflare(string? cfConnectingIp)
        {
            // Arrange: rodando local
            var cabecalhos = new Dictionary<string, string>();
            if (cfConnectingIp is not null)
                cabecalhos["CF-Connecting-IP"] = cfConnectingIp;

            var context = CriarContext(SEGREDO, cabecalhos);
            context.Connection.RemoteIpAddress = IPAddress.Loopback;

            // Act
            var ip = context.ObterIpCliente();

            // Assert
            ip.Should().Be("127.0.0.1");
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static DefaultHttpContext CriarContext(string? segredoConfigurado, Dictionary<string, string> cabecalhos)
        {
            var configuracao = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [IpClienteExtension.ConfiguracaoSegredoProxy] = segredoConfigurado })
                .Build();

            var context = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection().AddSingleton<IConfiguration>(configuracao).BuildServiceProvider()
            };

            foreach (var (nome, valor) in cabecalhos)
                context.Request.Headers[nome] = valor;

            return context;
        }
    }
}
