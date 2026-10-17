using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(9, "Cria tabela estoque_movimentacoes")]
    public class Versao0000009 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("estoque_movimentacoes")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("variante_id").AsInt32().NotNullable()
                    .ForeignKey("fk_estoque_mov_variantes", "produto_variantes", "id")
                .WithColumn("tipo").AsString(20).NotNullable()          // Entrada | Saida
                .WithColumn("quantidade").AsInt32().NotNullable()
                .WithColumn("motivo").AsString(200).Nullable()
                .WithColumn("pedido_id").AsInt32().Nullable()
                    .ForeignKey("fk_estoque_mov_pedidos", "pedidos", "id")
                .WithColumn("criado_em").AsDateTime().NotNullable()
                    .WithDefault(SystemMethods.CurrentUTCDateTime);

            Execute.Sql("ALTER TABLE estoque_movimentacoes ADD CONSTRAINT ck_estoque_mov_quantidade CHECK (quantidade > 0);");

            Create.Index("ix_estoque_mov_variante").OnTable("estoque_movimentacoes").OnColumn("variante_id").Ascending();
            Create.Index("ix_estoque_mov_pedido").OnTable("estoque_movimentacoes").OnColumn("pedido_id").Ascending();
        }
    }
}