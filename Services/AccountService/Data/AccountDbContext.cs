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
                    Password = "XWVlzLc5K4xHQ5bfxcmyXKXX5zyUFPvFmDZHWmj9/dg=:73f25c0d147b4ac6968be8455c817b0d", //"Admin@123"
                    TenantId = 1,
                    LastModifiedAt = null
                });

        }
    }
}