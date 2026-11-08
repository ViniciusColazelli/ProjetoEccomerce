using Domain.Services.Armazenamento;

namespace Infrastructure.Services.Armazenamento
{
    // Fotos numa pasta do disco, servidas pela própria API em /imagens (ver ImagensLocaisExtension).
    // Só para Development e testes: no Render o disco é apagado a cada deploy.
    public class ArmazenamentoLocal : IArmazenamentoImagens
    {
        private readonly string _pasta;
        private readonly string _urlPublica;

        public ArmazenamentoLocal(string pastaAbsoluta, string urlPublica)
        {
            _pasta = Path.GetFullPath(pastaAbsoluta);
            _urlPublica = urlPublica.TrimEnd('/');
        }

        public async Task Salvar(string caminho, Stream conteudo, string contentType, CancellationToken cancellationToken = default)
        {
            var arquivo = CaminhoNoDisco(caminho);

            Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);

            // CreateNew: falha se o arquivo já existir, como o x-upsert=false do Supabase
            await using var destino = new FileStream(arquivo, FileMode.CreateNew, FileAccess.Write);
            await conteudo.CopyToAsync(destino, cancellationToken);
        }

        public Task Apagar(IEnumerable<string> caminhos, CancellationToken cancellationToken = default)
        {
            foreach (var caminho in caminhos)
                File.Delete(CaminhoNoDisco(caminho)); // não existir não é erro

            return Task.CompletedTask;
        }

        public string UrlPublica(string caminho)
        {
            CaminhoArmazenamento.Validar(caminho);
            return $"{_urlPublica}/{caminho}";
        }

        private string CaminhoNoDisco(string caminho)
        {
            CaminhoArmazenamento.Validar(caminho);
            return Path.Combine(_pasta, caminho.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
