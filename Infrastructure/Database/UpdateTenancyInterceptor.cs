using Infrastructure.Entities;
using Infrastructure.Tenancy;
using Infrastructure.Web;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Database
{
    public sealed class UpdateTenancyInterceptor(IServiceScopeFactory serviceScopeFactory): SaveChangesInterceptor
    {
        readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return base.SavingChangesAsync(eventData, result);
            var entries = context.ChangeTracker.Entries<TenancyEntity>();
            var tenantProvider = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ICurrentUserProvider>();
            var utcNow = DateTime.UtcNow;
            foreach (var entry in entries)
            {
                entry.Entity.TenantId = tenantProvider.TenantId;
            }
            return base.SavingChangesAsync(eventData, result);
        }
    }
}
