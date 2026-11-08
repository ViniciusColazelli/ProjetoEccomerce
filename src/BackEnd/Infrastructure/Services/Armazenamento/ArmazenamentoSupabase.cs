using Domain.Services.Armazenamento;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Infrastructure.Services.Armazenamento
{
    // Fotos no Supabase Storage, pela API REST (a mesma que o supabase-js usa).
    // O bucket é público: o navegador busca a foto direto do CDN do Supabase, sem passar pela API.
    public class ArmazenamentoSupabase : IArmazenamentoImagens
    {
        // os nomes nunca se repetem (GUID), então a foto pode ficar em cache por um ano
        private const string CACHE_UM_ANO = "max-age=31536000";

        private readonly HttpClient _http;
        private readonly ArmazenamentoConfig _config;

        // `http` vem do IHttpClientFactory; aqui ele recebe o endereço do projeto e a chave de serviço
        // (nos dois cabeçalhos, como o supabase-js faz)
        public ArmazenamentoSupabase(HttpClient http, ArmazenamentoConfig config)
        {
            _config = config;
            _http = http;
            _http.BaseAddress = new Uri(config.SupabaseUrl.TrimEnd('/') + "/");
            _http.DefaultRequestHeaders.Add("apikey", config.ChaveServico);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ChaveServico);
        }

        public async Task Salvar(string caminho, Stream conteudo, string contentType, CancellationToken cancellationToken = default)
        {
            CaminhoArmazenamento.Validar(caminho);

            using var requisicao = new HttpRequestMessage(HttpMethod.Post, $"storage/v1/object/{_config.Bucket}/{caminho}")
            {
                Content = new StreamContent(conteudo)
            };
            requisicao.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            requisicao.Headers.Add("x-upsert", "false");
            requisicao.Headers.Add("cache-control", CACHE_UM_ANO);

            using var resposta = await _http.SendAsync(requisicao, cancellationToken);
            await GarantirSucesso(resposta, $"gravar {caminho}", cancellationToken);
        }

        public async Task Apagar(IEnumerable<string> caminhos, CancellationToken cancellationToken = default)
        {
            var lista = caminhos.ToList();
            if (lista.Count == 0)
                return;

            lista.ForEach(CaminhoArmazenamento.Validar);

            // arquivo que não existe é ignorado pelo Supabase
            using var requisicao = new HttpRequestMessage(HttpMethod.Delete, $"storage/v1/object/{_config.Bucket}")
            {
                Content = JsonContent.Create(new { prefixes = lista })
            };

            using var resposta = await _http.SendAsync(requisicao, cancellationToken);
            await GarantirSucesso(resposta, $"apagar {string.Join(", ", lista)}", cancellationToken);
        }

        public string UrlPublica(string caminho)
        {
            CaminhoArmazenamento.Validar(caminho);
            return $"{_config.SupabaseUrl.TrimEnd('/')}/storage/v1/object/public/{_config.Bucket}/{caminho}";
        }

        // A mensagem vai para o log da API (o cliente recebe o erro genérico do ExceptionFiltro); nunca inclui a chave
        private static async Task GarantirSucesso(HttpResponseMessage resposta, string acao, CancellationToken cancellationToken)
        {
            if (resposta.IsSuccessStatusCode)
                return;

            var corpo = await resposta.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Supabase Storage recusou {acao}: {(int)resposta.StatusCode} {corpo}");
        }
    }
}
