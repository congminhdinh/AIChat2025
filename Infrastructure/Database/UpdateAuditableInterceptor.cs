using Infrastructure.Entities;
using Infrastructure.Web;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Database
{
    public sealed class UpdateAuditableInterceptor(IServiceScopeFactory serviceScopeFactory): SaveChangesInterceptor
    {
        readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return base.SavingChangesAsync(eventData, result);
            var entries = context.ChangeTracker.Entries<AuditableEntity>();
            var currentUserService = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ICurrentUserProvider>();
            var utcNow = DateTime.UtcNow;
            foreach (var entry in entries)
            {
                if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                {
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.CreatedBy = currentUserService.Username;
                }
                else if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Modified)
                {
                    entry.Entity.LastModifiedAt = utcNow;
                    entry.Entity.LastModifiedBy = currentUserService.Username;
                }
                else if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Deleted)
                {
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.LastModifiedAt = utcNow;
                    entry.Entity.LastModifiedBy = currentUserService.Username;
                }

            }
            return base.SavingChangesAsync(eventData, result);
        }
    }
}
