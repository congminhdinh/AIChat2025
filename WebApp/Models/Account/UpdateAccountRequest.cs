namespace WebApp.Models.Account
{
    public class UpdateWebAppAccountRequest
    {
        public int AccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsChanged { get; set; }
        public string? Avatar { get; set; }
        public IFormFile? NewAvatar { get; set; } = null;
    }
}
