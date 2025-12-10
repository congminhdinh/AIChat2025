namespace AccountService.Dtos
{
    public class AccountDto
    {
        public AccountDto(int id, string name, string email, string? avatarUrl, bool isActive, bool isDisable)
        {
            Id = id;
            Name = name;
            Email = email;
            AvatarUrl = avatarUrl;
            IsActive = isActive;
            IsDisable = isDisable;
        }
        public int Id { get; set; }
        public string? Name { get; set; }
        public string Email { get; set; }
        public string? AvatarUrl { get; set; }
        public List<string> PermissionList { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public bool IsDisable { get; set; }
    }
}
