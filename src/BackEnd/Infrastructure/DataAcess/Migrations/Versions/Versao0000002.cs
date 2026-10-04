using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(2, "Cria tabela produtos")]
    public class Versao0000002 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("produtos")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("nome").AsString(200).NotNullable()
                .WithColumn("descricao").AsString(1000).Nullable()
                .WithColumn("preco_base").AsDecimal(10, 2).NotNullable()
                .WithColumn("ativo").AsBoolean().NotNullable().WithDefaultValue(true);
        }
    }
}