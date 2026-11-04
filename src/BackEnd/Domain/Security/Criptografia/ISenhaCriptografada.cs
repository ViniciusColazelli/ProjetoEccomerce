namespace Domain.Security.Criptografia
{
    // Senhas gravadas em BCrypt
    public interface ISenhaCriptografada
    {
        public string Gerar(string senha);

        // hashSalvo nulo (e-mail sem conta): faz uma verificação de mesmo custo e devolve false,
        // para o tempo de resposta não revelar quais e-mails estão cadastrados
        public bool Verificar(string senha, string? hashSalvo);

        // true quando o hash foi gerado com custo menor que o configurado (o login regrava com o custo atual)
        public bool PrecisaAtualizar(string hashSalvo);
    }
}
