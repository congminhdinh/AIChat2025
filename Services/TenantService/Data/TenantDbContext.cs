using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using TenantService.Entities;

namespace TenantService.Data
{
    public class TenantDbContext : BaseDbContext
    {
        public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
        {
        }
        public DbSet<Tenant> Tenants { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Tenant>().HasData(
                new Tenant
                {
                    Id = 1,
                    Name = "SuperAdmin"

                });

        }
    }
}
