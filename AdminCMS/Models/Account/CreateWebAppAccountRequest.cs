using Infrastructure;

namespace AdminCMS.Models.Account
{
    public class CreateWebAppAccountRequest : BaseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsChanged { get; set; } = false;
        public IFormFile? NewAvatar { get; set; }
    }
}
