namespace Domain.Security.Criptografia
{
    // Senhas novas são gravadas em BCrypt. Hashes SHA-512 antigos ainda são aceitos na verificação
    // e devem ser regravados (PrecisaAtualizar) assim que o cliente entrar com a senha certa.
    public interface ISenhaCriptografada
    {
        public string Gerar(string senha);

        // hashSalvo nulo (e-mail sem conta): faz uma verificação de mesmo custo e devolve false,
        // para o tempo de resposta não revelar quais e-mails estão cadastrados
        public bool Verificar(string senha, string? hashSalvo);

        // true para hash SHA-512 antigo ou BCrypt com custo menor que o configurado
        public bool PrecisaAtualizar(string hashSalvo);
    }
}
