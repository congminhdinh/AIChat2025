using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;
using System.Reflection;

namespace Infrastructure.Database
{
    public class BaseDbContext : DbContext
    {
        public BaseDbContext(DbContextOptions options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            //foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            //{
            //    if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType) && !entityType.IsAbstract())
            //    {
            //        // Use HasQueryFilter with lambda
            //        var method = typeof(ModelBuilder)
            //            .GetMethods()
            //            .First(m => m.Name == nameof(ModelBuilder.Entity) && m.IsGenericMethod);

            //        var generic = method.MakeGenericMethod(entityType.ClrType);
            //        var builder = generic.Invoke(modelBuilder, null);

            //        var builderType = typeof(EntityTypeBuilder<>).MakeGenericType(entityType.ClrType);
            //        var hasQueryFilter = builderType.GetMethod(nameof(EntityTypeBuilder.HasQueryFilter));

            //        // Build: e => !e.IsDeleted
            //        var parameter = Expression.Parameter(entityType.ClrType, "e");
            //        var property = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
            //        var condition = Expression.Not(property);
            //        var lambda = Expression.Lambda(condition, parameter);

            //        hasQueryFilter!.Invoke(builder, new object[] { lambda });
            //    }
            //}
        }
    }
}
