using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Business;
using WebApp.Requests;
using WebApp.Services;

namespace WebApp.Controllers
{
    public class AuthController: Controller
    {
        private readonly AuthBusiness _authBusiness;
        private readonly IJwtTokenParser _jwtTokenParser;

        public AuthController(AuthBusiness authBusiness, IJwtTokenParser jwtTokenParser)
        {
            _authBusiness = authBusiness;
            _jwtTokenParser = jwtTokenParser;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<JsonResult> ExecuteLogin([FromBody] LoginRequest input, CancellationToken cancellationToken)
        {
            try
            {
                // Call AuthBusiness to login
                var response = await _authBusiness.LoginAsync(input, cancellationToken);

                // Check if login was successful
                if (response.Status == BaseResponseStatus.Success && response.Data != null)
                {
                    var tokenData = response.Data;

                    // Parse JWT to extract claims using the injected TokenClaimsService
                    var jwtClaims = _jwtTokenParser.ParseAccessToken(tokenData.AccessToken);

                    // Create claims for the authenticated user (do not manually add token claims)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, input.Email)
                    };

                    // Add JWT claims (UserId, Username, TenantId, Scope, IsAdmin)
                    claims.AddRange(jwtClaims);

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // Store the raw Access Token in AuthenticationProperties
                    // This is required for the infrastructure's DelegatingHandler to forward the token
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Keep user logged in across browser sessions
                        ExpiresUtc = tokenData.ExpiresAt
                    };

                    // Store tokens using StoreTokens
                    authProperties.StoreTokens(new[]
                    {
                        new AuthenticationToken { Name = "access_token", Value = tokenData.AccessToken },
                        new AuthenticationToken { Name = "refresh_token", Value = tokenData.RefreshToken },
                        new AuthenticationToken { Name = "expires_at", Value = tokenData.ExpiresAt.ToString("o") }
                    });

                    // Sign in the user with cookie authentication
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties
                    );

                    // Return success response
                    return Json(new
                    {
                        success = true,
                        message = "Đăng nhập thành công",
                        redirectUrl = "/Account"
                    });
                }
                else
                {
                    // Login failed - return error message
                    return Json(new
                    {
                        success = false,
                        message = response.Message ?? "Tên đăng nhập hoặc mật khẩu không đúng"
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the error and return generic error message
                return Json(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại."
                });
            }
        }
    }
}
