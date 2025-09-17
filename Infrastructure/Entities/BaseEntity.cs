namespace Infrastructure.Entities
{
    public abstract class BaseEntity
    {
        public virtual int Id { get; set; }
    }
    public abstract class AuditableEntity : BaseEntity
    {
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? LastModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
    }

    public abstract class TenancyEntity : AuditableEntity
    {
        public int TenantId { get; set; }
    }
}
