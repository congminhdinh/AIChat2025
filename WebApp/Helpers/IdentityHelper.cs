using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

        public async Task SetAuthen(string accessToken)
        {
            var claimsDictionary = TokenDecoder.DecodeJwtToken(accessToken);
            var claims = new List<Claim>();

            foreach (var kvp in claimsDictionary)
            {
                if (kvp.Value is IEnumerable<object> list && !(kvp.Value is string))
                {
                    foreach (var item in list)
                    {
                        claims.Add(new Claim(kvp.Key, item?.ToString() ?? string.Empty));
                    }
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
                }
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(7)
            };

            authProperties.StoreTokens(new List<AuthenticationToken>
            {
                new AuthenticationToken
                {
                    Name = "access_token",
                    Value = accessToken
                }
            });

            await Current.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);
        }

        public bool IsAuthenticated()
        {
            return Current?.User?.Identity?.IsAuthenticated ?? false;
        }

        public async Task RemoveAuthen()
        {
            await Current.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }


        public int GetUserId()
        {
            var idClaim = Current?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim))
            {
                idClaim = Current?.User?.FindFirstValue(AuthorizationConstants.TOKEN_CLAIMS_USER);
            }

            return int.TryParse(idClaim, out int userId) ? userId : 0;
        }

        public int GetTenantId()
        {
            var tenantClaim = Current?.User?.FindFirstValue(AuthorizationConstants.TOKEN_CLAIMS_TENANT);
            return int.TryParse(tenantClaim, out int tenantId) ? tenantId : 0;
        }

        public string GetUsername()
        {
            return Current?.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        }

        public bool IsAdmin()
        {
            var isAdminClaim = Current?.User?.FindFirstValue(AuthorizationConstants.POLICY_ADMIN);
            return string.Equals(isAdminClaim, "True", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            if (Current == null) return null;

            var token = await Current.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }

            var authHeader = Current.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring(7);
            }

            return null;
        }
    }
}
