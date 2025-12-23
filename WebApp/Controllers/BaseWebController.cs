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

        /// <summary>
        /// Check if current user is an admin (matches service pattern)
        /// </summary>
        protected bool CheckIsAdmin()
        {
            return _currentUserProvider.IsAdmin;
        }

        /// <summary>
        /// Check if current user is super admin (TenantId=1 + IsAdmin)
        /// </summary>
        protected bool CheckIsSuperAdmin()
        {
            var tenantId = _currentUserProvider.TenantId;
            var isAdmin = _currentUserProvider.IsAdmin;
            return tenantId == 1 && isAdmin;
        }

        /// <summary>
        /// Get current user ID from claims
        /// </summary>
        protected int GetCurrentUserId()
        {
            return _currentUserProvider.UserId;
        }

        /// <summary>
        /// Get current tenant ID from claims
        /// </summary>
        protected int GetCurrentTenantId()
        {
            return _currentUserProvider.TenantId;
        }

        /// <summary>
        /// Handle unauthorized access consistently
        /// </summary>
        protected IActionResult UnauthorizedAccess(string message = "Bạn không có quyền truy cập trang này")
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction("Login", "Auth");
        }
    }
}
