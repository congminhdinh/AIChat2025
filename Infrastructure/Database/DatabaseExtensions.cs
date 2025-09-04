using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Database
{
    public static class DatabaseExtensions
    {
        public static void AddCustomDbContext<TContext>(this IHostApplicationBuilder builder, string connectionString, string dbSchema) where TContext : BaseDbContext
        {
            builder.Services.AddSingleton<UpdateAuditableInterceptor>();
            builder.Services.AddSingleton<UpdateTenancyInterceptor>();
            builder.Services.AddDbContext<TContext>((serviceProvider, optionBuilder) =>
            {
                optionBuilder.UseSqlite(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", dbSchema);
                });
                optionBuilder.AddInterceptors(
                    serviceProvider.GetRequiredService<UpdateAuditableInterceptor>(),
                    serviceProvider.GetRequiredService<UpdateTenancyInterceptor>()
                );
            });
        }
    }

    
}
