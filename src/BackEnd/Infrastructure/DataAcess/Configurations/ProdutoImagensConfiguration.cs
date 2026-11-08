using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAcess.Configurations
{
    public class ProdutoImagensConfiguration : IEntityTypeConfiguration<ProdutoImagens>
    {
        public void Configure(EntityTypeBuilder<ProdutoImagens> builder)
        {
            // o banco preenche criado_em (default da migration 17); o EF não envia o valor no INSERT
            builder.Property(imagem => imagem.CriadoEm)
                .HasDefaultValueSql("timezone('utc', now())");
        }
    }
}
