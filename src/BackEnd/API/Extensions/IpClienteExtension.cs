using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace API.Extensions
{
    // Descobre o IP de quem fez a requisição, para o rate limit separar um usuário do outro.
    // Em produção a requisição chega por dois caminhos, e cada um precisa de um cabeçalho diferente:
    //   - direto no Render: Cloudflare → API. O CF-Connecting-IP é o cliente e não dá para falsificar
    //     (o Cloudflare recusa a requisição que tenta enviar esse cabeçalho).
    //   - pelo site: Vercel (/backend) → Cloudflare → API. O CF-Connecting-IP vira o IP do servidor da Vercel,
    //     compartilhado por muitos usuários; o cliente é o 1º item do X-Forwarded-For, que a Vercel sobrescreve.
    //     Só que direto no Render esse mesmo item é falsificável, então ele só vale com o segredo que a Vercel envia.
    public static class IpClienteExtension
    {
        public const string CabecalhoSegredoProxy = "X-Proxy-Segredo";
        public const string ConfiguracaoSegredoProxy = "Proxy:Segredo";

        public static string ObterIpCliente(this HttpContext context)
        {
            var cabecalhos = context.Request.Headers;

            if (VeioDoProxy(context))
            {
                var primeiroEncaminhado = cabecalhos["X-Forwarded-For"].ToString().Split(',')[0].Trim();

                if (IPAddress.TryParse(primeiroEncaminhado, out _))
                    return primeiroEncaminhado;
            }

            var ipCloudflare = cabecalhos["CF-Connecting-IP"].ToString().Trim();

            if (IPAddress.TryParse(ipCloudflare, out _))
                return ipCloudflare;

            // rodando local, sem Cloudflare na frente
            return context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        }

        private static bool VeioDoProxy(HttpContext context)
        {
            var segredoEsperado = context.RequestServices.GetRequiredService<IConfiguration>()[ConfiguracaoSegredoProxy];
            var segredoRecebido = context.Request.Headers[CabecalhoSegredoProxy].ToString();

            if (string.IsNullOrEmpty(segredoEsperado) || string.IsNullOrEmpty(segredoRecebido))
                return false;

            // comparação em tempo constante: o tempo da resposta não revela quantos caracteres acertaram
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(segredoRecebido),
                Encoding.UTF8.GetBytes(segredoEsperado));
        }
    }
}
