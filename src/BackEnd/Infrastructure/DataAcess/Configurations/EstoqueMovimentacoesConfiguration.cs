using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAcess.Configurations
{
    public class EstoqueMovimentacoesConfiguration : IEntityTypeConfiguration<EstoqueMovimentacoes>
    {
        public void Configure(EntityTypeBuilder<EstoqueMovimentacoes> builder)
        {
            builder.Property(movimentacao => movimentacao.Tipo).HasConversion<string>();

            // o banco preenche criado_em (default da migration 9); o EF não envia o valor no INSERT
            builder.Property(movimentacao => movimentacao.CriadoEm)
                .HasDefaultValueSql("timezone('utc', now())");

            builder.HasOne(movimentacao => movimentacao.Variante)
                .WithMany()
                .HasForeignKey(movimentacao => movimentacao.VarianteId);
        }
    }
}
