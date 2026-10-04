using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(5, "Cria tabela pedido_itens")]
    public class Versao0000005 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("pedido_itens")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("pedido_id").AsInt32().NotNullable()
                    .ForeignKey("FK_pedido_itens_pedidos", "pedidos", "id")
                .WithColumn("produto_id").AsInt32().NotNullable()
                    .ForeignKey("FK_pedido_itens_produtos", "produtos", "id")
                .WithColumn("quantidade").AsInt32().NotNullable()
                .WithColumn("preco_unitario").AsDecimal(10, 2).NotNullable()
                .WithColumn("logo_url").AsString(500).Nullable();
        }
    }
}