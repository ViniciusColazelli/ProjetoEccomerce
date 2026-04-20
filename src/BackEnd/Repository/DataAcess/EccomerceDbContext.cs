using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess
{
    public class EccomerceDbContext : DbContext
    {
        public EccomerceDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Clientes> clientes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EccomerceDbContext).Assembly);
        }
    }
}
