using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(8, "Cria tabela estoque")]
    public class Versao0000008 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("estoque")
                .WithColumn("variante_id").AsInt32().PrimaryKey()
                    .ForeignKey("fk_estoque_variantes", "produto_variantes", "id")
                .WithColumn("quantidade").AsInt32().NotNullable().WithDefaultValue(0);

            // o banco nunca aceita saldo negativo
            Execute.Sql("ALTER TABLE estoque ADD CONSTRAINT ck_estoque_quantidade CHECK (quantidade >= 0);");
        }
    }
}