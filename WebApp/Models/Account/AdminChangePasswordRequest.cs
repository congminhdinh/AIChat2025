using Infrastructure;

namespace WebApp.Models.Account
{
    public class AdminChangePasswordRequest : BaseRequest
    {
        public int AccountId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
