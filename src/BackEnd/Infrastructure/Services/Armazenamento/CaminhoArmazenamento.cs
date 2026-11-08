using System.Text.RegularExpressions;

namespace Infrastructure.Services.Armazenamento
{
    // Os caminhos são montados pela própria API (pasta do produto + GUID), mas a checagem fica aqui também:
    // um caminho com ".." ou começando em "/" poderia gravar ou apagar fora da pasta das fotos
    internal static partial class CaminhoArmazenamento
    {
        // segmentos com letras, números, ponto, hífen e sublinhado, separados por "/"
        [GeneratedRegex(@"^[A-Za-z0-9_-][A-Za-z0-9._-]*(/[A-Za-z0-9_-][A-Za-z0-9._-]*)*$")]
        private static partial Regex Formato();

        public static void Validar(string caminho)
        {
            if (string.IsNullOrWhiteSpace(caminho) || Formato().IsMatch(caminho) == false || caminho.Contains(".."))
                throw new ArgumentException($"Caminho de arquivo inválido: '{caminho}'.", nameof(caminho));
        }
    }
}
