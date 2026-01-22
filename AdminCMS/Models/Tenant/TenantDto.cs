using Infrastructure;

namespace AdminCMS.Models.Tenant
{
    public class TenantDto
    {
        public TenantDto(int id, string name, string? description, bool isActive, DateTime createdAt, DateTime modifiedAt)
        {
            Id = id;
            Name = name;
            Description = description;
            IsActive = isActive;
            CreatedAt = createdAt;
            ModifiedAt = modifiedAt;
        }

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
