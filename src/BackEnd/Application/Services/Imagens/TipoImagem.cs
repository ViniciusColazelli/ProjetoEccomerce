namespace Application.Services.Imagens
{
    // Descobre o tipo da foto pelos primeiros bytes (a "assinatura" do formato), e não pelo nome
    // ou pelo Content-Type, que quem chama a API pode mandar errado. Só JPEG, PNG e WebP passam.
    public static class TipoImagem
    {
        public record Detectado(string ContentType, string Extensao);

        private static readonly Detectado Jpeg = new("image/jpeg", ".jpg");
        private static readonly Detectado Png = new("image/png", ".png");
        private static readonly Detectado Webp = new("image/webp", ".webp");

        // O stream precisa permitir voltar ao início: depois de ler a assinatura, o arquivo inteiro é gravado
        public static async Task<Detectado?> Detectar(Stream conteudo, CancellationToken cancellationToken = default)
        {
            var inicio = new byte[12];
            var lidos = await conteudo.ReadAtLeastAsync(inicio, inicio.Length, throwOnEndOfStream: false, cancellationToken);
            conteudo.Position = 0;

            var assinatura = inicio.AsSpan(0, lidos);

            // JPEG: FF D8 FF
            if (assinatura.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
                return Jpeg;

            // PNG: 89 'P' 'N' 'G' \r \n 1A \n
            if (assinatura.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                return Png;

            // WebP: "RIFF", 4 bytes de tamanho, "WEBP"
            if (lidos >= 12 && assinatura[..4].SequenceEqual("RIFF"u8) && assinatura[8..12].SequenceEqual("WEBP"u8))
                return Webp;

            return null;
        }
    }
}
