using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    public class ChatController: Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
