using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAcess.Configurations
{
    public class ProdutosConfiguration : IEntityTypeConfiguration<Produtos>
    {
        public void Configure(EntityTypeBuilder<Produtos> builder)
        {
            builder.HasMany(produto => produto.Opcoes)
                .WithOne()
                .HasForeignKey(opcao => opcao.ProdutoId);

            builder.HasMany(produto => produto.Variantes)
                .WithOne()
                .HasForeignKey(variante => variante.ProdutoId);
        }
    }
}
