using Infrastructure;
using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;
using WebApp.Business;
using WebApp.Helpers;
using WebApp.Models.Chat;

namespace WebApp.Controllers
{
    public class ChatController : BaseWebController
    {
        private readonly ChatBusiness _chatBusiness;
        private readonly IdentityHelper _identityHelper;

        public ChatController(ICurrentUserProvider currentUserProvider, ChatBusiness chatBusiness, IdentityHelper identityHelper)
            : base(currentUserProvider)
        {
            _chatBusiness = chatBusiness;
            _identityHelper = identityHelper;
        }

        /// <summary>
        /// Returns the main Chat View
        /// </summary>
        public IActionResult Index()
        {
            // Check if user is authenticated
            if (!_identityHelper.IsAuthenticated())
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        /// <summary>
        /// Returns JSON data for the conversation list (sidebar)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var response = await _chatBusiness.GetConversationsAsync();

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data });
        }

        /// <summary>
        /// Returns JSON data for the message history of a specific conversation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHistory(int id)
        {
            var response = await _chatBusiness.GetMessageHistoryAsync(id);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data });
        }

        /// <summary>
        /// Sends a new message and returns the bot's response
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Json(new { success = false, message = "Nội dung tin nhắn không được để trống" });
            }

            var response = await _chatBusiness.SendMessageAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data });
        }
    }
}
