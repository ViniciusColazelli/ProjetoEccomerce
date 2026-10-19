namespace Application.Request
{
    public class RequestRegistrarOpcao
    {
        public string Tipo { get; set; } = string.Empty; // Tamanho | Cor | Personalizacao
        public string Valor { get; set; } = string.Empty;
        public decimal PrecoAdicional { get; set; }
    }
}
