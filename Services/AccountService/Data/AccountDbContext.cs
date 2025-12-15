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
                    Password = "7Qp+0J3/1Zc/u/O8T/uFzO/o6uX/iT4/5z/0q/3z/q8=:b6a9876543210fedcba9876543210fed", //"Admin@123"
                    TenantId = 1,
                    LastModifiedAt = null
                });
            modelBuilder.Entity<Account>().HasQueryFilter(a => a.TenancyActive && a.IsActive && !a.IsDeleted);
        }
    }
}