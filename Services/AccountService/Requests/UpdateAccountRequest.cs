using Infrastructure;

namespace AccountService.Requests
{
    public class UpdateAccountRequest: BaseRequest
    {
        public int AccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Avatar { get; set; }
    }
}
