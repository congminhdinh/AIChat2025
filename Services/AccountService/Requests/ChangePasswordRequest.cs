using Infrastructure;

namespace AccountService.Requests
{
    public class ChangePasswordRequest: BaseRequest
    {
        public int AccountId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }
}
