using Infrastructure;
using Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Business;
using WebApp.Helpers;
using WebApp.Models.Chat;

namespace WebApp.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly ChatBusiness _chatBusiness;
        private readonly IdentityHelper _identityHelper;

        public DocumentController(ChatBusiness chatBusiness, IdentityHelper identityHelper)
        {
            _chatBusiness = chatBusiness;
            _identityHelper = identityHelper;
        }

        public IActionResult Index()
        {
            return View();
        }

        
    }
}
