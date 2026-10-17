using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAcess.Configurations
{
    public class EstoqueConfiguration : IEntityTypeConfiguration<Estoque>
    {
        public void Configure(EntityTypeBuilder<Estoque> builder)
        {
            // a chave é a própria variante (não há coluna id), e o valor vem dela, não do banco
            builder.HasKey(estoque => estoque.VarianteId);
            builder.Property(estoque => estoque.VarianteId).ValueGeneratedNever();
        }
    }
}
