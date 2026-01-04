using Infrastructure;

namespace AdminCMS.Models.Tenant
{
    public class CreateTenantRequest : BaseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;  // Hidden from UI, defaults to true
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? AccountName { get; set; }
        public List<int> PermissionsList { get; set; } = new List<int>();  // Empty list from UI
    }
}
