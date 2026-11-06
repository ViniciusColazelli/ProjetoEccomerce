using FluentMigrator;

namespace Infrastructure.DataAcess.Migrations.Versions
{
    [Migration(16, "Garante que só personalização tem preço adicional em produto_opcoes")]
    public class Versao0000016 : ForwardOnlyMigration
    {
        public override void Up()
        {
            // o preço de tamanho e cor nunca entrou no pedido (CriarPedidoUseCase), então zerar não muda nenhum total
            Execute.Sql("UPDATE produto_opcoes SET preco_adicional = 0 WHERE tipo <> 'Personalizacao' AND preco_adicional <> 0;");

            // mesma regra do TipoOpcaoExtension.PodeTerPrecoAdicional, para nada gravado fora da API escapar
            Execute.Sql(@"ALTER TABLE produto_opcoes ADD CONSTRAINT ck_produto_opcoes_preco_adicional
                          CHECK (tipo = 'Personalizacao' OR preco_adicional = 0);");
        }
    }
}
