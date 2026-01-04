using Infrastructure;

namespace AdminCMS.Models.Account
{
    public class UpdateWebAppAccountRequest : BaseRequest
    {
        public int AccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsChanged { get; set; } = false;
        public string? Avatar { get; set; }
        public IFormFile? NewAvatar { get; set; }
    }
}
