using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApp.Business;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Models.Account;

namespace WebApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly AccountBusiness _accountBusiness;
        private readonly IdentityHelper _identityHelper;
        private readonly AppSettings _appSettings;

        public AccountController(AccountBusiness accountBusiness, IdentityHelper identityHelper, IOptions<AppSettings> appSettings)
        {
            _accountBusiness = accountBusiness;
            _identityHelper = identityHelper;
            _appSettings = appSettings.Value;
        }

        public IActionResult Index()
        {
            // Global [Authorize] filter handles authentication
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAccounts([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var request = new GetAccountListRequest
            {
                Name = keyword,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            var response = await _accountBusiness.GetListAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return PartialView("_AccountList", new PaginatedListDto<AccountDto>
                {
                    Items = new List<AccountDto>(),
                    PageIndex = 1,
                    TotalPages = 0,
                    PageSize = 0
                });
            }

            // Pass ImageBaseUrl to ViewBag for avatar rendering
            ViewBag.ImageBaseUrl = _appSettings.ImageBaseUrl;

            return PartialView("_AccountList", response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAccountById(int id)
        {
            var response = await _accountBusiness.GetByIdAsync(id);

            if (response.Status == BaseResponseStatus.Error || response.Data == null)
            {
                return Json(new { success = false, message = response.Message ?? "Không tìm thấy tài khoản" });
            }

            var imageBaseUrl = _appSettings.ImageBaseUrl ?? string.Empty;
            var avatarUrl = response.Data.AvatarUrl ?? string.Empty;
            var fullAvatarUrl = !string.IsNullOrEmpty(avatarUrl)
                ? imageBaseUrl + avatarUrl
                : "images/default-avatar.jpg";

            var viewModel = new AccountDetailViewModel
            {
                AccountId = id,
                AccountName = response.Data.Name ?? string.Empty,
                IsActive = response.Data.IsActive,
                AvatarUrl = avatarUrl,
                ImageBaseUrl = imageBaseUrl,
                FullAvatarUrl = fullAvatarUrl
            };

            return PartialView("GetAccountDetail", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromForm] UpdateWebAppAccountRequest request)
        {
            if (request.AccountId <= 0)
            {
                return Json(new { success = false, message = "ID tài khoản không hợp lệ" });
            }

            var response = await _accountBusiness.UpdateAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data, message = "Cập nhật tài khoản thành công" });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "ID tài khoản không hợp lệ" });
            }

            var response = await _accountBusiness.DeleteAsync(id);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, message = "Xóa tài khoản thành công" });
        }

        [HttpGet]
        public IActionResult GetChangePasswordModal()
        {
            return PartialView("ChangePasswordPartial");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // Validate passwords match
            if (string.IsNullOrEmpty(request.NewPassword))
            {
                return Json(new { success = false, message = "Vui lòng nhập mật khẩu mới" });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp" });
            }

            if (request.NewPassword.Length < 6)
            {
                return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự" });
            }

            var response = await _accountBusiness.ChangePasswordAsync(request.NewPassword);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, message = "Đổi mật khẩu thành công" });
        }
    }
}
