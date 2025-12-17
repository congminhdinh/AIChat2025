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

            // Automatically apply all IEntityTypeConfiguration classes from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Loop through all entities discovered by EF Core
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Check if the entity type is a class that inherits from AuditableEntity
                if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType) && entityType.ClrType.IsClass)
                {
                    // If so, add the soft-delete query filter
                    AddSoftDeleteFilter(modelBuilder, entityType.ClrType);
                }
            }
        }

        private void AddSoftDeleteFilter(ModelBuilder modelBuilder, Type entityType)
        {
            var parameter = Expression.Parameter(entityType, "m");
            var property = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parameter);
            modelBuilder.Entity(entityType).HasQueryFilter(lambda);
        }
    }
}
