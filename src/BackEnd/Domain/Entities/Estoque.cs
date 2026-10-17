namespace Domain.Entities
{
    // Saldo atual da variante (uma linha por variante; o banco não aceita negativo)
    public class Estoque
    {
        public int VarianteId { get; set; }
        public int Quantidade { get; set; }
    }
}
