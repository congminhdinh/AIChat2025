using Infrastructure;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Business;
using WebApp.Helpers;
using WebApp.Requests;

namespace WebApp.Controllers
{
    public class AuthController: Controller
    {
        private readonly AuthBusiness _authBusiness;
        private readonly IdentityHelper _identityHelper;

        public AuthController(AuthBusiness authBusiness, IdentityHelper identityHelper)
        {
            _authBusiness = authBusiness;
            _identityHelper = identityHelper;
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
                var response = await _authBusiness.LoginAsync(input, cancellationToken);

                // Check if login was successful
                if (response.Status == BaseResponseStatus.Success && response.Data != null)
                {
                    await _identityHelper.SetAuthen(
                        tenantId: response.Data.TenantId,
                        userId: response.Data.UserId,
                        username: response.Data.Username,
                        scope: response.Data.Scope,
                        isAdmin: response.Data.IsAdmin,
                        accessToken: response.Data.AccessToken
                    );
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

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _identityHelper.RemoveAuthen();
            return RedirectToAction("Login", "Auth");
        }
    }
}
