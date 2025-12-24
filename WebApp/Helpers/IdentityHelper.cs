using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace WebApp.Helpers
{
    public class IdentityHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public IdentityHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        private HttpContext Current => _httpContextAccessor.HttpContext;
        public async Task SetAuthen(int tenantId, int userId, string username, string scope, bool isAdmin)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(AuthorizationConstants.TOKEN_CLAIMS_TENANT, tenantId.ToString()),
                new Claim(AuthorizationConstants.POLICY_ADMIN, isAdmin == false? "False": "True"),
                new(AuthorizationConstants.TOKEN_CLAIMS_TYPE_SCOPE, $"{scope}"),
                new(AuthorizationConstants.TOKEN_CLAIMS_TENANT, $"{tenantId}"),
            };
            var identity = new ClaimsIdentity(claims, "AIChat2025");
            var principal = new ClaimsPrincipal(identity);

            await Current.SignInAsync("AIChat2025", principal);

        }

        public async Task RemoveAuthen()
        {
            await Current.SignOutAsync("AIChat2025");
        }
    }
}
