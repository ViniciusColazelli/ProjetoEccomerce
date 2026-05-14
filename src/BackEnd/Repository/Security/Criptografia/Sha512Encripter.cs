using Domain.Security.Criptografia;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security.Criptografia
{
    public class Sha512Encripter : ISenhaCriptografada
    {
        public string Criptografia(string senha)
        {
            var bytes = Encoding.UTF8.GetBytes(senha);
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
