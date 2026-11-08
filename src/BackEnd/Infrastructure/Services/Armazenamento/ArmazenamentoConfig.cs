using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services.Armazenamento
{
    // Seção "Storage" da configuração. Em homologação e produção vem das variáveis de ambiente do Render:
    //   Storage__Provedor=Supabase, Storage__SupabaseUrl, Storage__ChaveServico (secreta) e Storage__Bucket
    public class ArmazenamentoConfig
    {
        public const string Secao = "Storage";
        public const string ProvedorLocal = "Local";
        public const string ProvedorSupabase = "Supabase";

        // Rota em que a API serve as fotos quando o provedor é o disco local
        public const string RotaImagensLocais = "/imagens";

        public string Provedor { get; set; } = ProvedorLocal;

        // Disco local: pasta (relativa ao projeto da API, ou absoluta) e o endereço que o navegador usa
        public string PastaLocal { get; set; } = "imagens";
        public string UrlPublicaLocal { get; set; } = "https://localhost:7252" + RotaImagensLocais;

        // Supabase Storage: URL do projeto (https://xxxx.supabase.co), bucket público e chave de serviço
        public string SupabaseUrl { get; set; } = string.Empty;
        public string Bucket { get; set; } = "produtos";
        public string ChaveServico { get; set; } = string.Empty;

        public bool UsaSupabase => string.Equals(Provedor, ProvedorSupabase, StringComparison.OrdinalIgnoreCase);

        public static ArmazenamentoConfig Ler(IConfiguration configuration)
        {
            var config = configuration.GetSection(Secao).Get<ArmazenamentoConfig>() ?? new ArmazenamentoConfig();

            var provedorValido = config.UsaSupabase ||
                string.Equals(config.Provedor, ProvedorLocal, StringComparison.OrdinalIgnoreCase);

            if (provedorValido == false)
                throw new InvalidOperationException($"Storage:Provedor inválido ('{config.Provedor}'). Use {ProvedorLocal} ou {ProvedorSupabase}.");

            // melhor a API não subir do que subir e falhar no primeiro envio de foto
            if (config.UsaSupabase &&
                (string.IsNullOrWhiteSpace(config.SupabaseUrl) || string.IsNullOrWhiteSpace(config.ChaveServico) || string.IsNullOrWhiteSpace(config.Bucket)))
                throw new InvalidOperationException("Com Storage:Provedor=Supabase, configure Storage:SupabaseUrl, Storage:ChaveServico e Storage:Bucket.");

            return config;
        }

        public string PastaLocalAbsoluta(string contentRootPath) =>
            Path.GetFullPath(Path.Combine(contentRootPath, PastaLocal));
    }
}
