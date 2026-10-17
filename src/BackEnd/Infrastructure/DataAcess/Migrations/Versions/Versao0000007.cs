using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(7, "Cria tabela produto_variantes")]
    public class Versao0000007 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("produto_variantes")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("produto_id").AsInt32().NotNullable()
                    .ForeignKey("fk_produto_variantes_produtos", "produtos", "id")
                .WithColumn("tamanho_opcao_id").AsInt32().NotNullable()
                    .ForeignKey("fk_produto_variantes_tamanho", "produto_opcoes", "id")
                .WithColumn("cor_opcao_id").AsInt32().Nullable()
                    .ForeignKey("fk_produto_variantes_cor", "produto_opcoes", "id")
                .WithColumn("ativo").AsBoolean().NotNullable().WithDefaultValue(true);

            // impede duplicar a mesma combinação (NULLS NOT DISTINCT exige PostgreSQL 15+)
            Execute.Sql(@"CREATE UNIQUE INDEX ux_produto_variantes_combinacao
                          ON produto_variantes (produto_id, tamanho_opcao_id, cor_opcao_id) NULLS NOT DISTINCT;");

            Create.Index("ix_produto_variantes_tamanho").OnTable("produto_variantes").OnColumn("tamanho_opcao_id").Ascending();
            Create.Index("ix_produto_variantes_cor").OnTable("produto_variantes").OnColumn("cor_opcao_id").Ascending();
        }
    }
}