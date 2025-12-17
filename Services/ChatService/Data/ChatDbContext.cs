using ChatService.Entities;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Data
{
    public class ChatDbContext : BaseDbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
        {
        }

        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<PromptConfig> PromptConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ChatConversation configuration
            modelBuilder.Entity<ChatConversation>(entity =>
            {
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasIndex(e => new { e.TenantId, e.UserId });
                entity.HasIndex(e => e.LastMessageAt);

                entity.HasQueryFilter(c => !c.IsDeleted);
            });

            // ChatMessage configuration
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.Property(e => e.Message)
                    .IsRequired();

                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => new { e.TenantId, e.UserId });

                entity.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasQueryFilter(m => !m.IsDeleted);
            });

            // PromptConfig configuration
            modelBuilder.Entity<PromptConfig>(entity =>
            {
                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Value)
                    .IsRequired();

                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Key);

                entity.HasQueryFilter(p => !p.IsDeleted);
            });
        }
    }
}
