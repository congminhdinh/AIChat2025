namespace AccountService.Dtos
{
    public class AccountDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int TenantId { get; set; }
        public string? AvatarUrl { get; set; }
        public List<string> PermissionList { get; set; }
        public 
    }
}
