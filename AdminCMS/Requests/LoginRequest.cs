using Infrastructure;

namespace AdminCMS.Requests
{
    public class LoginRequest: BaseRequest
    {
        public string TenantKey { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}