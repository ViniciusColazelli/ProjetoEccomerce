using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAcess.Configurations
{
    public class ProdutoOpcoesConfiguration : IEntityTypeConfiguration<ProdutoOpcoes>
    {
        public void Configure(EntityTypeBuilder<ProdutoOpcoes> builder)
        {
            builder.Property(opcao => opcao.Tipo).HasConversion<string>();
        }
    }
}
