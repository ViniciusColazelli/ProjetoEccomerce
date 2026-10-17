using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess
{
    public class EccomerceDbContext : DbContext
    {
        public EccomerceDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Clientes> Clientes { get; set; }
        public DbSet<Produtos> Produtos { get; set; }

        // o nome do DbSet vira o nome da tabela (ToSnakeCase abaixo): ProdutoOpcoes -> produto_opcoes
        public DbSet<ProdutoOpcoes> ProdutoOpcoes { get; set; }
        public DbSet<ProdutoVariantes> ProdutoVariantes { get; set; }
        public DbSet<Estoque> Estoque { get; set; }
        public DbSet<EstoqueMovimentacoes> EstoqueMovimentacoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EccomerceDbContext).Assembly);

            // Configurações para converter nomes no banco de dados para snake_case
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(ToSnakeCase(entity.GetTableName()!));

                foreach (var property in entity.GetProperties())
                    property.SetColumnName(ToSnakeCase(property.GetColumnName()!));

                foreach (var key in entity.GetKeys())
                    key.SetName(ToSnakeCase(key.GetName()!));

                foreach (var fk in entity.GetForeignKeys())
                    fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName()!));
            }
        }
        private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return string.Concat(name.Select((c, i) =>
                i > 0 && char.IsUpper(c) ? "_" + c.ToString() : c.ToString()
            )).ToLower();
        }
    }
}
