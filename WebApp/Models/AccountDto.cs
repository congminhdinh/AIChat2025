namespace WebApp.Models
{
    public class AccountDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string Email { get; set; }
        public string? AvatarUrl { get; set; }
        public List<int> PermissionList { get; set; } = new List<int>();
        public bool IsActive { get; set; }
        public bool IsDisable { get; set; }
    }
}
