using Domain.Security.Criptografia;

namespace Infrastructure.Security.Criptografia
{
    // Hashes BCrypt: sal aleatório por senha e custo configurável. Todo hash válido começa com "$2" (ex.: "$2a$11$...").
    public class BCryptSenha : ISenhaCriptografada
    {
        public const int FATOR_CUSTO_PADRAO = 11;

        private readonly int _fatorCusto;
        private readonly string _hashFicticio;

        public BCryptSenha(int fatorCusto)
        {
            _fatorCusto = fatorCusto;

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

            try
            {
                return BCrypt.Net.BCrypt.Verify(senha, hashSalvo);
            }
            catch (Exception)
            {
                // hash que não é BCrypt ou está corrompido (a biblioteca lança exceções diferentes conforme o defeito):
                // trata como senha errada, nunca como erro 500
                return false;
            }
        }

        public bool PrecisaAtualizar(string hashSalvo)
        {
            try
            {
                return BCrypt.Net.BCrypt.PasswordNeedsRehash(hashSalvo, _fatorCusto);
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
