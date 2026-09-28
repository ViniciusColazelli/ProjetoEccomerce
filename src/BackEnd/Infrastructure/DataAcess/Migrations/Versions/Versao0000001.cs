using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(1, "Cria tabela clientes")]
    public class Versao0000001 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("clientes")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("nome").AsString(100).NotNullable()
                .WithColumn("email").AsString(200).NotNullable().Unique()
                .WithColumn("senha").AsString(200).NotNullable();
        }
    }
}
