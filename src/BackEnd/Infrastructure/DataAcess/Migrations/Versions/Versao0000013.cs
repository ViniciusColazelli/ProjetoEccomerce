using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(13, "Torna o e-mail de clientes único sem diferenciar maiúsculas")]
    public class Versao0000013 : ForwardOnlyMigration
    {
        public override void Up()
        {
            // a API já salva o e-mail em minúsculas e sem espaços; padroniza os que foram gravados antes
            Execute.Sql(@"UPDATE clientes SET email = lower(trim(email)) WHERE email <> lower(trim(email));");

            // remove o único simples da Versao0000001 ("Ana@x.com" e "ana@x.com" passavam como diferentes).
            // O nome foi gerado automaticamente, então procura pelo índice único que cobre só a coluna email
            Execute.Sql(@"DO $$
                          DECLARE r record;
                          BEGIN
                              FOR r IN
                                  SELECT indice.relname AS indice, restricao.conname AS restricao
                                  FROM pg_index x
                                  JOIN pg_class indice ON indice.oid = x.indexrelid
                                  JOIN pg_class tabela ON tabela.oid = x.indrelid
                                  JOIN pg_attribute coluna ON coluna.attrelid = tabela.oid AND coluna.attnum = x.indkey[0]
                                  LEFT JOIN pg_constraint restricao ON restricao.conindid = x.indexrelid
                                  WHERE tabela.relname = 'clientes'
                                    AND tabela.relnamespace = 'public'::regnamespace
                                    AND x.indisunique
                                    AND x.indnatts = 1
                                    AND coluna.attname = 'email'
                              LOOP
                                  IF r.restricao IS NOT NULL THEN
                                      EXECUTE format('ALTER TABLE clientes DROP CONSTRAINT %I', r.restricao);
                                  ELSE
                                      EXECUTE format('DROP INDEX %I', r.indice);
                                  END IF;
                              END LOOP;
                          END $$;");

            // o banco passa a recusar o mesmo e-mail com outra capitalização, mesmo em cadastros simultâneos;
            // o login (lower(email) = @email) também usa este índice
            Execute.Sql(@"CREATE UNIQUE INDEX ux_clientes_email ON clientes (lower(email));");
        }
    }
}
