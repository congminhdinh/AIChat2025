using DocumentService.Entities;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Data
{
    public class DocumentDbContext : BaseDbContext
    {
        public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options)
        {

        }
        public DbSet<PromptDocument> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
        }
    }
}