using Domain.Enums;

namespace Domain.Extensions
{
    // Só personalização muda o preço da peça: o pedido cobra o preço base + personalizações (CriarPedidoUseCase).
    // Tamanho e cor ficam com preço adicional 0, e o banco também garante isso (CHECK da migration 16)
    public static class TipoOpcaoExtension
    {
        public static bool PodeTerPrecoAdicional(this TipoOpcao tipo) => tipo == TipoOpcao.Personalizacao;
    }
}
