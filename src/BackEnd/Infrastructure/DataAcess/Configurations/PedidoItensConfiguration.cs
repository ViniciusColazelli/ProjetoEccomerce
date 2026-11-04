using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAcess.Configurations
{
    public class PedidoItensConfiguration : IEntityTypeConfiguration<PedidoItens>
    {
        public void Configure(EntityTypeBuilder<PedidoItens> builder)
        {
            // Restrict: um pedido nunca some porque o produto, a variante ou a opção foram apagados
            builder.HasOne(item => item.Produto)
                .WithMany()
                .HasForeignKey(item => item.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(item => item.Variante)
                .WithMany()
                .HasForeignKey(item => item.VarianteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(item => item.Customizacoes)
                .WithOne()
                .HasForeignKey(customizacao => customizacao.PedidoItemId);
        }
    }
}
