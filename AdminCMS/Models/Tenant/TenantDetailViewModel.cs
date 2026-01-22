namespace AdminCMS.Models.Tenant
{
    public class TenantDetailViewModel
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
