using Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Authorize]
    public class BaseWebController : Controller
    {
        protected readonly ICurrentUserProvider _currentUserProvider;

        public BaseWebController(ICurrentUserProvider currentUserProvider)
        {
            _currentUserProvider = currentUserProvider;
        }
        protected bool CheckIsAdmin()
        {
            return _currentUserProvider.IsAdmin;
        }

        protected bool CheckIsSuperAdmin()
        {
            var tenantId = _currentUserProvider.TenantId;
            var isAdmin = _currentUserProvider.IsAdmin;
            return tenantId == 1 && isAdmin;
        }

        protected int GetCurrentUserId()
        {
            return _currentUserProvider.UserId;
        }

        protected int GetCurrentTenantId()
        {
            return _currentUserProvider.TenantId;
        }

        protected IActionResult UnauthorizedAccess(string message = "Bạn không có quyền truy cập trang này")
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction("Login", "Auth");
        }
    }
}
