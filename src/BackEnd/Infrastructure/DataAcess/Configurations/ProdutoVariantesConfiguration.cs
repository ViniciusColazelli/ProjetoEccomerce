using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAcess.Configurations
{
    public class ProdutoVariantesConfiguration : IEntityTypeConfiguration<ProdutoVariantes>
    {
        public void Configure(EntityTypeBuilder<ProdutoVariantes> builder)
        {
            // duas FKs para a mesma tabela (produto_opcoes): precisam ser declaradas explicitamente.
            // Restrict evita que o EF apague variantes em cascata ao remover uma opção.
            builder.HasOne(variante => variante.TamanhoOpcao)
                .WithMany()
                .HasForeignKey(variante => variante.TamanhoOpcaoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(variante => variante.CorOpcao)
                .WithMany()
                .HasForeignKey(variante => variante.CorOpcaoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(variante => variante.Estoque)
                .WithOne()
                .HasForeignKey<Estoque>(estoque => estoque.VarianteId);
        }
    }
}
