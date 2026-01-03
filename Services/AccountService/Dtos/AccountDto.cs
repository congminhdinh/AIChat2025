namespace AccountService.Dtos
{
    public class AccountDto
    {
        public AccountDto(int id, string name, string email, string? avatarUrl, bool isActive, string? permissions)
        {
            Id = id;
            Name = name;
            Email = email;
            AvatarUrl = avatarUrl;
            IsActive = isActive;
            PermissionList = string.IsNullOrEmpty(permissions) ? new List<int>() : permissions.Split(',').Select(m => Int32.Parse(m)).ToList();
        }
        public int Id { get; set; }
        public string? Name { get; set; }
        public string Email { get; set; }
        public string? AvatarUrl { get; set; }
        public List<int> PermissionList { get; set; } = new List<int>();
        public bool IsActive { get; set; }
    }
}
