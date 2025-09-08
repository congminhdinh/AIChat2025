using AccountService.Entities;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Data
{
    public class AccountDbContext: BaseDbContext
    {
        public AccountDbContext(DbContextOptions<AccountDbContext> options): base(options)
        {
            
        }
        public DbSet<Account> Accounts { get; set; }
    }
}
