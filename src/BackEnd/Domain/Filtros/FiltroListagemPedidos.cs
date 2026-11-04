using Domain.Enums;

namespace Domain.Filtros
{
    // Usado pelo cliente (sempre com o ClienteId dele) e pelo admin
    public class FiltroListagemPedidos
    {
        public int? ClienteId { get; set; }
        public StatusPedido? Status { get; set; }
        public string? BuscaCliente { get; set; } // parte do nome ou do e-mail, sem diferenciar maiúsculas
        public DateOnly? De { get; set; }
        public DateOnly? Ate { get; set; } // inclusivo: vale o dia inteiro
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 10;
    }
}
