using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(14, "Prepara pedidos: observação, atualizado_em, regras de status e valores, e índices")]
    public class Versao0000014 : ForwardOnlyMigration
    {
        public override void Up()
        {
            Alter.Table("pedidos")
                .AddColumn("observacao").AsString(500).Nullable()          // recado do cliente no pedido
                .AddColumn("atualizado_em").AsDateTime().Nullable();      // última mudança de status

            // o status vira enum no código, então o texto gravado precisa bater exatamente com o nome
            Execute.Sql(@"ALTER TABLE pedidos ADD CONSTRAINT ck_pedidos_status
                          CHECK (status IN ('AguardandoPagamento', 'Pago', 'EmProducao', 'ProntoParaRetirada', 'Entregue', 'Cancelado'));");

            // o servidor calcula os valores, e o banco garante que nenhum fica negativo
            Execute.Sql("ALTER TABLE pedidos ADD CONSTRAINT ck_pedidos_total CHECK (total >= 0);");
            Execute.Sql("ALTER TABLE pedido_itens ADD CONSTRAINT ck_pedido_itens_quantidade CHECK (quantidade > 0);");
            Execute.Sql("ALTER TABLE pedido_itens ADD CONSTRAINT ck_pedido_itens_preco CHECK (preco_unitario >= 0);");
            Execute.Sql("ALTER TABLE pedido_item_customizacoes ADD CONSTRAINT ck_pedido_item_customizacoes_preco CHECK (preco_adicional >= 0);");

            // o Postgres não cria índice para FK sozinho
            Create.Index("ix_pedidos_cliente").OnTable("pedidos").OnColumn("cliente_id").Ascending();                 // meus pedidos
            Create.Index("ix_pedidos_status_criado").OnTable("pedidos")                                               // listagem do admin
                .OnColumn("status").Ascending()
                .OnColumn("criado_em").Descending();
            Create.Index("ix_pedido_itens_pedido").OnTable("pedido_itens").OnColumn("pedido_id").Ascending();
            Create.Index("ix_pedido_item_customizacoes_item").OnTable("pedido_item_customizacoes").OnColumn("pedido_item_id").Ascending();
            Create.Index("ix_pedido_item_customizacoes_opcao").OnTable("pedido_item_customizacoes").OnColumn("opcao_id").Ascending();  // opção em uso
        }
    }
}
