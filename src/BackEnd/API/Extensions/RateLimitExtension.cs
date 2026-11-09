using Application.Response;
using Exceptions;
using System.Threading.RateLimiting;

namespace API.Extensions
{
    public static class RateLimitExtension
    {
        public const string PoliticaAutenticacao = "autenticacao";
        public const string PoliticaRenovacao = "renovacao";

        public static void AddRateLimitConfigurado(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Limite geral: vale para todas as rotas
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ObterIp(context),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // Limite estrito: só nas rotas marcadas com [EnableRateLimiting("autenticacao")]
                options.AddPolicy(PoliticaAutenticacao, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ObterIp(context),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // Refresh da sessão: cada aba aberta renova sozinha, então o limite do login (5) derrubaria
                // quem usa várias abas. 30 por minuto ainda impede tentar tokens em massa
                options.AddPolicy(PoliticaRenovacao, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ObterIp(context),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // Resposta no mesmo formato ResponseErro do resto da API,
                // com a mensagem vinda do ResourceMensagensDeErro.resx
                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString();
                    }

                    context.HttpContext.Response.ContentType = "application/json";

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new ResponseErro(ResourceMensagensDeErro.MUITAS_REQUISICOES),
                        cancellationToken);
                };
            });
        }
        private static string ObterIp(HttpContext context) => context.ObterIpCliente();
    }
}