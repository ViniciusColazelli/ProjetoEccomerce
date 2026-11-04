using Domain.Security.Criptografia;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security.Criptografia
{
    // Gera hashes BCrypt (sal aleatório por senha, custo configurável) e ainda verifica o formato SHA-512 antigo.
    // Hash BCrypt sempre começa com "$2" (ex.: "$2a$11$..."); o antigo é só hexadecimal.
    public class BCryptSenha : ISenhaCriptografada
    {
        public const int FATOR_CUSTO_PADRAO = 11;

        private readonly int _fatorCusto;
        private readonly Sha512Encripter _formatoAntigo;
        private readonly string _hashFicticio;

        public BCryptSenha(int fatorCusto, Sha512Encripter formatoAntigo)
        {
            _fatorCusto = fatorCusto;
            _formatoAntigo = formatoAntigo;

            // usado quando o e-mail não tem conta: verificar contra ele custa o mesmo que uma senha de verdade
            _hashFicticio = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), fatorCusto);
        }

        public string Gerar(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, _fatorCusto);

        public bool Verificar(string senha, string? hashSalvo)
        {
            // "senha": null no JSON do login chega aqui como nulo
            senha ??= string.Empty;

            if (string.IsNullOrEmpty(hashSalvo))
            {
                BCrypt.Net.BCrypt.Verify(senha, _hashFicticio);

                return false;
            }

            if (EhBCrypt(hashSalvo) == false)
                return VerificarFormatoAntigo(senha, hashSalvo);

            try
            {
                return BCrypt.Net.BCrypt.Verify(senha, hashSalvo);
            }
            catch (Exception)
            {
                // hash começando com "$2" mas corrompido (a biblioteca lança exceções diferentes conforme o defeito):
                // trata como senha errada, nunca como erro 500
                return false;
            }
        }

        public bool PrecisaAtualizar(string hashSalvo)
        {
            if (EhBCrypt(hashSalvo) == false)
                return true;

            try
            {
                return BCrypt.Net.BCrypt.PasswordNeedsRehash(hashSalvo, _fatorCusto);
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static bool EhBCrypt(string hash) => hash.StartsWith("$2", StringComparison.Ordinal);

        // comparação em tempo constante, como na verificação do BCrypt
        private bool VerificarFormatoAntigo(string senha, string hashSalvo) =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(_formatoAntigo.Criptografia(senha)),
                Encoding.UTF8.GetBytes(hashSalvo));
    }
}
