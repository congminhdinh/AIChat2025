using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Infrastructure.Authentication
{
    public interface ITokenClaimsService
    {
        TokenResponseDto GetTokenAsync(int tenantId, int userId, string username, string scope, bool isAdmin);
    }
    public class TokenClaimsService : ITokenClaimsService
    {
        public TokenResponseDto GetTokenAsync(int tenantId, int userId, string username, string scope, bool isAdmin)
        {
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(AuthorizationConstants.JWT_SECRET_KEY));
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(AuthorizationConstants.TOKEN_CLAIMS_TENANT, tenantId.ToString()),
                new Claim(AuthorizationConstants.POLICY_ADMIN, isAdmin == false? "False": "True"),
                new(AuthorizationConstants.TOKEN_CLAIMS_TYPE_SCOPE, $"{scope}"),
                new(AuthorizationConstants.TOKEN_CLAIMS_TENANT, $"{tenantId}"),
            };
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(7);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                SigningCredentials = creds
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            return new TokenResponseDto(
                AccessToken: tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor)),
                RefreshToken: Guid.NewGuid().ToString().Replace("-", ""),
                ExpiresIn: (long)(expires - DateTime.Now).TotalSeconds,
                ExpiresAt: expires
            );
        }
        
    }
    public record TokenResponseDto(string AccessToken, string RefreshToken, long ExpiresIn, DateTime ExpiresAt);
}
