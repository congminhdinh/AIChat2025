using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;
using WebApp.Business;

namespace WebApp.Controllers
{
    public class AccountController : BaseWebController
    {
        private readonly ILogger<AccountController> _logger;
        private readonly PermissionBusiness _permissionBusiness;

        public AccountController(
            ICurrentUserProvider currentUserProvider,
            ILogger<AccountController> logger,
            PermissionBusiness permissionBusiness)
            : base(currentUserProvider)
        {
            _logger = logger;
            _permissionBusiness = permissionBusiness;
        }

        public IActionResult Index()
        {
            try
            {
                // Debug: Log user claims
                _logger.LogInformation($"User authenticated: {User.Identity?.IsAuthenticated}");
                _logger.LogInformation($"User ID: {_currentUserProvider.UserId}");
                _logger.LogInformation($"User IsAdmin: {_currentUserProvider.IsAdmin}");
                _logger.LogInformation($"User TenantId: {_currentUserProvider.TenantId}");

                // Use shared authorization pattern from services
                if (!CheckIsAdmin())
                {
                    _logger.LogWarning("User is not admin, redirecting to login");
                    return UnauthorizedAccess("Chỉ quản trị viên mới có quyền truy cập");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Account/Index: {ex.Message}");
                throw;
            }
        }
    }
}
