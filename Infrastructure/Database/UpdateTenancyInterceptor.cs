using Infrastructure.Entities;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Database
{

    public sealed class UpdateTenancyInterceptor(IServiceScopeFactory serviceScopeFactory) : SaveChangesInterceptor
    {
        readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return base.SavingChangesAsync(eventData, result);

            var entries = context.ChangeTracker.Entries<TenancyEntity>();

            using var scope = _serviceScopeFactory.CreateScope();
            var tenantProvider = scope.ServiceProvider.GetRequiredService<ICurrentTenantProvider>();

            var currentTenantId = tenantProvider.TenantId;

            if (currentTenantId > 0)
            {
                foreach (var entry in entries)
                {
                    entry.Entity.TenantId = currentTenantId;
                }
            }

            return base.SavingChangesAsync(eventData, result);
        }
    }
}
