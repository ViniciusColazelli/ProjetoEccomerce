using Microsoft.Extensions.Configuration;

namespace Infrastructure.Security.Tokens
{
    // Seção "Sessao" da configuração (appsettings.json). No Render pode ser trocada por
    // Sessao__ExpiracaoEmDias e Sessao__DuracaoMaximaEmDias
    public class SessaoConfig
    {
        public const string Secao = "Sessao";

        // sem entrar no site por esse tempo, precisa logar de novo
        public int ExpiracaoEmDias { get; set; } = 7;

        // mesmo usando sempre, precisa logar de novo depois desse tempo
        public int DuracaoMaximaEmDias { get; set; } = 30;

        public static SessaoConfig Ler(IConfiguration configuration)
        {
            var config = configuration.GetSection(Secao).Get<SessaoConfig>() ?? new SessaoConfig();

            if (config.ExpiracaoEmDias <= 0 || config.DuracaoMaximaEmDias < config.ExpiracaoEmDias)
                throw new InvalidOperationException(
                    "Sessao:ExpiracaoEmDias precisa ser maior que zero e Sessao:DuracaoMaximaEmDias, maior ou igual a ela.");

            return config;
        }
    }
}
