using AdminCMS.Business;
using AdminCMS.Helpers;
using AdminCMS.Requests;
using Infrastructure;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminCMS.Controllers
{
    [AllowAnonymous]
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
                    await _identityHelper.SetAuthen(response.Data.AccessToken);
                    return Json(new
                    {
                        success = true,
                        message = "Đăng nhập thành công",
                        redirectUrl = "/Tenant"
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
