namespace Infrastructure.Entities
{
    public abstract class BaseEntity
    {
        public virtual int Id { get; set; }
    }
    public abstract class AuditableEntity : BaseEntity
    {
        public DateTime Created { get; set; }
        public DateTime? LastModified { get; set; }
        public string? CreatedBy { get; set; }
        public string? LastModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
