using Infrastructure;
using Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApp.Business;
using WebApp.Helpers;

namespace WebApp.Controllers
{
    [Authorize]
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
            // Global [Authorize] filter handles authentication
            return View();
        }
    }
}
