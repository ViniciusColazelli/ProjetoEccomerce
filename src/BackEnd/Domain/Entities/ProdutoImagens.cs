namespace Domain.Entities
{
    // Foto de um produto. Os arquivos ficam no Storage (IArmazenamentoImagens); aqui só os caminhos.
    // O navegador envia duas versões já reduzidas: a grande (página do produto) e a miniatura (cards e listas).
    public class ProdutoImagens
    {
        public int Id { get; set; }
        public int ProdutoId { get; set; }
        public string Caminho { get; set; } = string.Empty;
        public string CaminhoMiniatura { get; set; } = string.Empty;
        public int Ordem { get; set; } // a menor é a capa
        public DateTime CriadoEm { get; set; }
    }
}
