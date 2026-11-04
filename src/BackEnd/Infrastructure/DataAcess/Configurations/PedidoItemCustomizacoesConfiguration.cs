using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAcess.Configurations
{
    public class PedidoItemCustomizacoesConfiguration : IEntityTypeConfiguration<PedidoItemCustomizacoes>
    {
        public void Configure(EntityTypeBuilder<PedidoItemCustomizacoes> builder)
        {
            builder.Property(customizacao => customizacao.Tipo).HasConversion<string>();

            builder.HasOne(customizacao => customizacao.Opcao)
                .WithMany()
                .HasForeignKey(customizacao => customizacao.OpcaoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
