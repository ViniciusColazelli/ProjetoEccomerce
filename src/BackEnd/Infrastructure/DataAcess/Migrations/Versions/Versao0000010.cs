using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(10, "Adiciona variante_id em pedido_itens")]
    public class Versao0000010 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Alter.Table("pedido_itens")
                .AddColumn("variante_id").AsInt32().Nullable();

            Create.ForeignKey("fk_pedido_itens_variantes")
                .FromTable("pedido_itens").ForeignColumn("variante_id")
                .ToTable("produto_variantes").PrimaryColumn("id");

            Create.Index("ix_pedido_itens_variante").OnTable("pedido_itens").OnColumn("variante_id").Ascending();
        }
    }
}