using Infrastructure.Entities;

namespace TenantService.Entities
{
    public class Tenant: AuditableEntity
    {
        public  Tenant()
        {
            
        }
        public Tenant(string name, string? description, bool isActive)
        {
            Name = name;
            Description = description;
            IsActive = isActive;
        }

        public string Name{ get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Permissions { get; set; }
    }
}
