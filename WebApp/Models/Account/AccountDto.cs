namespace WebApp.Models.Account
{
    public class AccountDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string Email { get; set; }
        public string? AvatarUrl { get; set; }
        public List<int> PermissionList { get; set; } = new List<int>();
        public bool IsActive { get; set; }
    }
}
