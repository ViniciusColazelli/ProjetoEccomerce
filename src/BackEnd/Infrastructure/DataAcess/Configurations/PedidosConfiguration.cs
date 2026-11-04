using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAcess.Configurations
{
    public class PedidosConfiguration : IEntityTypeConfiguration<Pedidos>
    {
        public void Configure(EntityTypeBuilder<Pedidos> builder)
        {
            builder.Property(pedido => pedido.Status).HasConversion<string>();

            // o banco preenche criado_em (default da migration 4); o EF não envia o valor no INSERT
            builder.Property(pedido => pedido.CriadoEm)
                .HasDefaultValueSql("timezone('utc', now())");

            // dois admins confirmando o pagamento ao mesmo tempo baixariam o estoque duas vezes:
            // com o xmin, a segunda gravação falha e vira 409
            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            builder.HasOne(pedido => pedido.Cliente)
                .WithMany()
                .HasForeignKey(pedido => pedido.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(pedido => pedido.Itens)
                .WithOne()
                .HasForeignKey(item => item.PedidoId);
        }
    }
}
