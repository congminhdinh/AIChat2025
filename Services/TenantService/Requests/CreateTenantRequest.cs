namespace TenantService.Requests
{
    public class CreateTenantRequest: BaseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? AccountName { get; set; } = string.Empty;
        public List<int> PermissionsList { get; set; } = new List<int>();
    }
}
