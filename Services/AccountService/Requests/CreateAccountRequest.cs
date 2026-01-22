using Infrastructure;

namespace AccountService.Requests
{
    public class CreateAccountRequest: BaseRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? Name { get; set; } = string.Empty;
        public List<int> PermissionsList { get; set; } = new List<int>();
    }
    public class CreateAdminAccountRequest : CreateAccountRequest
    {
        public int TenantId { get; set; }
    }
}
