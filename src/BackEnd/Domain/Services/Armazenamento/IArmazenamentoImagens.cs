namespace Domain.Services.Armazenamento
{
    // Onde ficam os arquivos das fotos dos produtos: disco local em Development e nos testes,
    // Supabase Storage em homologação e produção. O banco guarda só o caminho de cada arquivo.
    public interface IArmazenamentoImagens
    {
        // Grava o arquivo em `caminho` (ex.: "produtos/12/3f2a9c.webp"). Nunca sobrescreve um arquivo existente:
        // os nomes são únicos (GUID), então o navegador pode guardar a foto em cache para sempre
        Task Salvar(string caminho, Stream conteudo, string contentType, CancellationToken cancellationToken = default);

        // Apagar um arquivo que já não existe não é erro
        Task Apagar(IEnumerable<string> caminhos, CancellationToken cancellationToken = default);

        // Endereço que o navegador usa para mostrar a foto
        string UrlPublica(string caminho);
    }
}
