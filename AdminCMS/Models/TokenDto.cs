namespace AdminCMS.Models
{
    public class TokenDto
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
        public int TenantId { get; init; }
        public int UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Scope { get; init; } = string.Empty;
        public bool IsAdmin { get; init; }
     }
}
