using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(4, "Cria tabela pedidos")]
    public class Versao0000004 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("pedidos")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("cliente_id").AsInt32().NotNullable()
                    .ForeignKey("FK_pedidos_clientes", "clientes", "id")
                .WithColumn("status").AsString(50).NotNullable()
                .WithColumn("total").AsDecimal(10, 2).NotNullable()
                .WithColumn("criado_em").AsDateTime().NotNullable()
                    .WithDefault(SystemMethods.CurrentUTCDateTime);
        }
    }
}