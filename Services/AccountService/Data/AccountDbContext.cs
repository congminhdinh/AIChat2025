using AccountService.Entities;
using Infrastructure.Database;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Data
{
    public class AccountDbContext : BaseDbContext
    {
        public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options)
        {

        }
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Account>().HasData(
                new Account
                {
                    Id = 1,
                    Email = "minhdc223@gmail.com",
                    Name = "Admin",
                    IsAdmin = true,
                    Password = "Xgz3816ok9rbQwhcSCYt00NH9qkEvWDdWiY9LH6fZy4=:ae98995e673341afb9f1932ec28c2c90", //"Admin@123"
                    TenantId = 1,
                    LastModifiedAt = null
                });
            modelBuilder.Entity<Account>().HasQueryFilter(a => a.TenancyActive && a.IsActive && !a.IsDeleted);
        }
    }
}