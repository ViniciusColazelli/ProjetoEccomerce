namespace Application.Request
{
    // Montado pelo controller a partir do multipart (campos "imagem" e "miniatura").
    // O navegador já envia as duas versões reduzidas; a API só confere e grava.
    public class RequestEnviarImagem
    {
        public ArquivoEnviado? Imagem { get; init; }
        public ArquivoEnviado? Miniatura { get; init; }
    }

    public class ArquivoEnviado
    {
        public required Stream Conteudo { get; init; }
        public long Tamanho { get; init; }
    }
}
