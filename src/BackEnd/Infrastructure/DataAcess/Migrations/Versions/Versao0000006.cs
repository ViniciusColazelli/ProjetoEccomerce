using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(6, "Cria tabela pedido_item_customizacoes")]
    public class Versao0000006 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("pedido_item_customizacoes")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("pedido_item_id").AsInt32().NotNullable()
                    .ForeignKey("FK_pedido_item_customizacoes_pedido_itens", "pedido_itens", "id")
                .WithColumn("opcao_id").AsInt32().NotNullable()
                    .ForeignKey("FK_pedido_item_customizacoes_produto_opcoes", "produto_opcoes", "id")
                .WithColumn("tipo").AsString(100).NotNullable()
                .WithColumn("valor").AsString(200).NotNullable()
                .WithColumn("preco_adicional").AsDecimal(10, 2).NotNullable().WithDefaultValue(0);
        }
    }
}