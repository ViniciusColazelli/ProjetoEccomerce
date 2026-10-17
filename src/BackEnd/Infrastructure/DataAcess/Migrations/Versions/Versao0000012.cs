using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(12, "Adiciona ativo e padroniza tipo em produto_opcoes")]
    public class Versao0000012 : ForwardOnlyMigration
    {
        public override void Up()
        {
            // desativar no lugar de excluir: variantes e pedidos referenciam a opção por FK
            Alter.Table("produto_opcoes")
                .AddColumn("ativo").AsBoolean().NotNullable().WithDefaultValue(true);

            // o tipo vira enum no código, então o texto gravado precisa bater exatamente com o nome
            Execute.Sql(@"UPDATE produto_opcoes SET tipo = 'Tamanho' WHERE lower(trim(tipo)) = 'tamanho';
                          UPDATE produto_opcoes SET tipo = 'Cor' WHERE lower(trim(tipo)) = 'cor';
                          UPDATE produto_opcoes SET tipo = 'Personalizacao' WHERE lower(trim(tipo)) IN ('personalizacao', 'personalização');");

            Execute.Sql(@"UPDATE estoque_movimentacoes SET tipo = 'Entrada' WHERE lower(trim(tipo)) = 'entrada';
                          UPDATE estoque_movimentacoes SET tipo = 'Saida' WHERE lower(trim(tipo)) IN ('saida', 'saída');");

            Execute.Sql(@"ALTER TABLE produto_opcoes ADD CONSTRAINT ck_produto_opcoes_tipo
                          CHECK (tipo IN ('Tamanho', 'Cor', 'Personalizacao'));");

            // impede cadastrar a mesma opção duas vezes no produto ("M" e "m" contam como iguais)
            Execute.Sql(@"CREATE UNIQUE INDEX ux_produto_opcoes_valor
                          ON produto_opcoes (produto_id, tipo, lower(valor));");
        }
    }
}
