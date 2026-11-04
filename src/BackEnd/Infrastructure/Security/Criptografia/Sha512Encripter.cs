using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security.Criptografia
{
    // Formato antigo das senhas: SHA-512 de "<senha> <AdditionalKey>", em hexadecimal.
    // Só serve para verificar contas que ainda não entraram desde a troca para BCrypt (BCryptSenha regrava no login).
    // Etapa 2 da migração: quando nenhuma conta tiver mais esse formato, excluir esta classe e a AdditionalKey.
    public class Sha512Encripter
    {
        private readonly string _additionalKey;
        public Sha512Encripter(string additionalKey)
        {
            _additionalKey = additionalKey;
        }
        public string Criptografia(string senha)
        {
            var novaSenha = $"{senha} {_additionalKey}";

            var bytes = Encoding.UTF8.GetBytes(novaSenha);
            var hashBytes = SHA512.HashData(bytes);

            return BytesParaString(hashBytes);
        }

        private static string BytesParaString(byte[] bytes) // metodo de transformar bytes em string
        {
            var stringBuilder = new StringBuilder();
            foreach (byte b in bytes)
            {
                var hex = b.ToString("x2");
                stringBuilder.Append(hex);
            }
            return stringBuilder.ToString();
        }
    }
}
