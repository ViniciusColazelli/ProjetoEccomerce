using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAcess.Configurations
{
    public class SessoesConfiguration : IEntityTypeConfiguration<Sessoes>
    {
        public void Configure(EntityTypeBuilder<Sessoes> builder)
        {
            // o banco preenche criado_em (default da migration 18); o EF não envia o valor no INSERT
            builder.Property(sessao => sessao.CriadoEm)
                .HasDefaultValueSql("timezone('utc', now())");

            // apagar o cliente apaga as sessões dele
            builder.HasOne(sessao => sessao.Cliente)
                .WithMany()
                .HasForeignKey(sessao => sessao.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // apagar a sessão seguinte (limpeza das vencidas) só desfaz a ligação
            builder.HasOne(sessao => sessao.SubstituidaPor)
                .WithMany()
                .HasForeignKey(sessao => sessao.SubstituidaPorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
