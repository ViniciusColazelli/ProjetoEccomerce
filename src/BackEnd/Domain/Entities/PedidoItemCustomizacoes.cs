using Domain.Enums;

namespace Domain.Entities
{
    // Personalização escolhida no item (ex.: Bordado +R$15). Tipo, valor e preço são copiados da opção
    // no momento da compra, para o pedido não mudar se a opção for editada depois.
    public class PedidoItemCustomizacoes
    {
        public int Id { get; set; }
        public int PedidoItemId { get; set; }
        public int OpcaoId { get; set; }
        public TipoOpcao Tipo { get; set; }
        public string Valor { get; set; } = string.Empty;
        public decimal PrecoAdicional { get; set; }

        public ProdutoOpcoes Opcao { get; set; } = null!;
    }
}
