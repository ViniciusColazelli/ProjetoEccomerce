using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(15, "Ativa a extensão unaccent para a busca por nome ignorar acentos")]
    public class Versao0000015 : ForwardOnlyMigration
    {
        public override void Up()
        {
            // unaccent('Calça') = 'Calca': quem digita "calca" encontra "Calça" (ProdutoRepository.Filtrar).
            // IF NOT EXISTS: não falha se a extensão já foi ativada pelo painel do Supabase
            Execute.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
        }
    }
}
