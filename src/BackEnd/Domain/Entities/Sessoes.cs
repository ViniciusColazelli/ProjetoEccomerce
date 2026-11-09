namespace Domain.Entities
{
    // Sessão de login, identificada pelo refresh token (só o hash fica no banco).
    // O access token (JWT) dura minutos; com a sessão ativa, o refresh emite outro sem pedir a senha.
    public class Sessoes
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public Clientes Cliente { get; set; } = null!;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }

        // já limitado pelo ExpiraMaximoEm: é também a validade do cookie
        public DateTime ExpiraEm { get; set; }

        // limite desde o login, herdado por todas as renovações
        public DateTime ExpiraMaximoEm { get; set; }

        public DateTime? RevogadoEm { get; set; }

        // a sessão que nasceu da renovação desta (rotação do refresh token)
        public int? SubstituidaPorId { get; set; }
        public Sessoes? SubstituidaPor { get; set; }

        public bool Ativa(DateTime agora) => RevogadoEm is null && agora < ExpiraEm;
    }
}
