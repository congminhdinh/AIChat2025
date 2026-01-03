namespace WebApp.Models.Account
{
    public class AccountDetailViewModel
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string ImageBaseUrl { get; set; } = string.Empty;
        public string FullAvatarUrl { get; set; } = string.Empty;
    }
}
