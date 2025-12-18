namespace AccountService.Dtos
{
    public class CurrentUserDto
    {
        public CurrentUserDto(int userId, int tenantId, string? username, string? scope, bool isAdmin, string? token)
        {
            UserId = userId;
            TenantId = tenantId;
            Username = username;
            Scope = scope;
            IsAdmin = isAdmin;
            Token = token;
        }

        public int UserId { get; set; }
        public int TenantId { get; set; }
        public string? Username { get; set; }
        public string? Scope { get; set; }
        public bool IsAdmin { get; set; }
        public string? Token { get; set; }
    }
}
