using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebApp.Services
{
    public interface IJwtTokenParser
    {
        List<Claim> ParseAccessToken(string accessToken);
    }

    public class JwtTokenParser : IJwtTokenParser
    {
        public List<Claim> ParseAccessToken(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(accessToken);
            return jwtToken.Claims.ToList();
        }
    }
}
