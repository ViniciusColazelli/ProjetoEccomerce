using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(17, "Cria tabela produto_imagens")]
    public class Versao0000017 : ForwardOnlyMigration
    {
        public override void Up()
        {
            // os arquivos ficam no Storage (Supabase em produção); aqui só os caminhos das duas versões
            Create.Table("produto_imagens")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("produto_id").AsInt32().NotNullable()
                    .ForeignKey("fk_produto_imagens_produtos", "produtos", "id")
                .WithColumn("caminho").AsString(300).NotNullable()             // versão grande
                .WithColumn("caminho_miniatura").AsString(300).NotNullable()   // cards e listas
                .WithColumn("ordem").AsInt32().NotNullable()                   // a menor é a capa
                .WithColumn("criado_em").AsDateTime().NotNullable()
                    .WithDefault(SystemMethods.CurrentUTCDateTime);

            Execute.Sql("ALTER TABLE produto_imagens ADD CONSTRAINT ck_produto_imagens_ordem CHECK (ordem >= 0);");

            // fotos de um produto, já na ordem (vitrine, detalhe e admin)
            Create.Index("ix_produto_imagens_produto_ordem").OnTable("produto_imagens")
                .OnColumn("produto_id").Ascending()
                .OnColumn("ordem").Ascending();
        }
    }
}
