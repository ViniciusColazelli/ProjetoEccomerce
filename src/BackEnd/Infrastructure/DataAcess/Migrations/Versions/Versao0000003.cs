using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(3, "Cria tabela produto_opcoes")]
    public class Versao0000003 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("produto_opcoes")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("produto_id").AsInt32().NotNullable()
                    .ForeignKey("FK_produto_opcoes_produtos", "produtos", "id")
                .WithColumn("tipo").AsString(100).NotNullable()
                .WithColumn("valor").AsString(200).NotNullable()
                .WithColumn("preco_adicional").AsDecimal(10, 2).NotNullable().WithDefaultValue(0);
        }
    }
}