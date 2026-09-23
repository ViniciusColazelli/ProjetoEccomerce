using Domain.Security.Criptografia;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security.Criptografia
{
    public class Sha512Encripter : ISenhaCriptografada
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
