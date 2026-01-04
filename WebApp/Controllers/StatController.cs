using Microsoft.AspNetCore.Mvc;
using WebApp.Business;

namespace WebApp.Controllers
{
    public class StatController: Controller
    {
        private readonly ChatFeedbackBusiness _chatFeedbackBusiness;

        public StatController(ChatFeedbackBusiness chatFeedbackBusiness)
        {
            _chatFeedbackBusiness = chatFeedbackBusiness;
        }

        public ActionResult Index()
        {
            return View();
        }
    }
}
