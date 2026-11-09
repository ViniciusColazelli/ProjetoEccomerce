using FluentMigrator;
using System.Data;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(18, "Cria tabela sessoes (refresh tokens)")]
    public class Versao0000018 : ForwardOnlyMigration
    {
        public override void Up()
        {
            // Uma linha por refresh token emitido. O token fica só no cookie do navegador; aqui vai o hash SHA-256
            // dele, para quem ler o banco não conseguir usar as sessões.
            // A cada renovação a linha é revogada e aponta para a seguinte (substituida_por_id): se um token
            // já trocado aparecer de novo, alguém copiou o cookie.
            Create.Table("sessoes")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("cliente_id").AsInt32().NotNullable()
                    .ForeignKey("fk_sessoes_clientes", "clientes", "id").OnDelete(Rule.Cascade)
                .WithColumn("token_hash").AsString(64).NotNullable()
                    .Unique("ux_sessoes_token_hash")
                .WithColumn("criado_em").AsDateTime().NotNullable()
                    .WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("expira_em").AsDateTime().NotNullable()          // sem uso por N dias: renova a cada refresh
                .WithColumn("expira_maximo_em").AsDateTime().NotNullable()   // limite desde o login: nunca é adiado
                .WithColumn("revogado_em").AsDateTime().Nullable()           // logout, troca de senha ou renovação
                .WithColumn("substituida_por_id").AsInt32().Nullable()
                    .ForeignKey("fk_sessoes_substituida_por", "sessoes", "id").OnDelete(Rule.SetNull);

            Execute.Sql("ALTER TABLE sessoes ADD CONSTRAINT ck_sessoes_expiracao CHECK (expira_em <= expira_maximo_em);");

            // sessões de um cliente (revogar todas, limite por cliente e limpeza das vencidas)
            Create.Index("ix_sessoes_cliente").OnTable("sessoes")
                .OnColumn("cliente_id").Ascending();
        }
    }
}
