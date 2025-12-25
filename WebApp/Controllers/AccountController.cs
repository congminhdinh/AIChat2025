using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;
using WebApp.Business;
using WebApp.Helpers;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly PermissionBusiness _permissionBusiness;
        private readonly IdentityHelper _identityHelper;

        public AccountController(ILogger<AccountController> logger, PermissionBusiness permissionBusiness, IdentityHelper identityHelper)
        {
            _logger = logger;
            _permissionBusiness = permissionBusiness;
            _identityHelper = identityHelper;
        }

        public IActionResult Index()
        {
            try
            {
                // Check if user is authenticated
                if (!_identityHelper.IsAuthenticated())
                {
                    _logger.LogWarning("User is not authenticated, redirecting to login");
                    return RedirectToAction("Login", "Auth");
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
