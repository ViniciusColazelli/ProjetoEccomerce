using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(11, "Adiciona perfil em clientes")]
    public class Versao0000011 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Alter.Table("clientes")
                .AddColumn("perfil").AsString(20).NotNullable().WithDefaultValue("Cliente");
        }
    }
}