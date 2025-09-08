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
            // Create the lambda parameter (e.g., "m")
            var parameter = Expression.Parameter(entityType, "m");

            // Get the "IsDeleted" property from the parameter
            var property = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));

            // Create the condition "m.IsDeleted == false"
            var condition = Expression.Equal(property, Expression.Constant(false));

            // Build the complete lambda expression: m => m.IsDeleted == false
            var lambda = Expression.Lambda(condition, parameter);

            // Apply the filter to the entity
            modelBuilder.Entity(entityType).HasQueryFilter(lambda);
        }
    }
}
