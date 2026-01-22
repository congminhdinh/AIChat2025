using Infrastructure;
using Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApp.Business;
using WebApp.Helpers;
using WebApp.Models.Chat;

namespace WebApp.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ChatBusiness _chatBusiness;
        private readonly ChatFeedbackBusiness _chatFeedbackBusiness;
        private readonly IdentityHelper _identityHelper;
        private readonly AppSettings _appSettings;

        public ChatController(ChatBusiness chatBusiness, ChatFeedbackBusiness chatFeedbackBusiness, IdentityHelper identityHelper, IOptions<AppSettings> appSettings)
        {
            _chatBusiness = chatBusiness;
            _chatFeedbackBusiness = chatFeedbackBusiness;
            _identityHelper = identityHelper;
            _appSettings = appSettings.Value;
        }

        public IActionResult Index()
        {
            // Global [Authorize] filter handles authentication
            // Pass ImageBaseUrl to view for document reference links
            ViewBag.ImageBaseUrl = _appSettings.ImageBaseUrl;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetInitialData()
        {
            var conversationsResponse = await _chatBusiness.GetConversationsAsync();

            if (conversationsResponse.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = conversationsResponse.Message });
            }

            var conversations = conversationsResponse.Data ?? new List<ConversationDto>();
            ConversationDto? firstConversation = null;

            if (conversations.Any())
            {
                var firstConversationId = conversations.First().Id;
                var conversationResponse = await _chatBusiness.GetConversationByIdAsync(firstConversationId);

                if (conversationResponse.Status == BaseResponseStatus.Success && conversationResponse.Data != null)
                {
                    firstConversation = conversationResponse.Data;
                }
            }

            return Json(new
            {
                success = true,
                conversations = conversations,
                currentConversation = firstConversation
            });
        }

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

        [HttpGet]
        public async Task<IActionResult> GetConversation(int id)
        {
            var response = await _chatBusiness.GetConversationByIdAsync(id);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data });
        }

        [HttpGet]
        public IActionResult CreateConversationPartial()
        {
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Json(new { success = false, message = "Tiêu đề không được để trống" });
            }

            var response = await _chatBusiness.CreateConversationAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { success = false, message = "Nội dung tin nhắn không được để trống" });
            }

            var response = await _chatBusiness.SendMessageAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, message = "OK" });
        }

        [HttpGet]
        public async Task<IActionResult> GetChatFeedback(int messageId)
        {
            var response = await _chatFeedbackBusiness.GetChatFeedbackDetailAsync(messageId);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data });
        }

        [HttpGet]
        public IActionResult ChatFeedbackPartial()
        {
            return PartialView("_ChatFeedbackModal");
        }

        [HttpPost]
        public async Task<IActionResult> RateChatFeedback([FromBody] RateChatFeedbackRequest request)
        {
            var response = await _chatFeedbackBusiness.RateChatFeedbackAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data });
        }

        [HttpPost]
        public async Task<IActionResult> CreateChatFeedback([FromBody] CreateChatFeedbackRequest request)
        {
            var response = await _chatFeedbackBusiness.CreateChatFeedbackAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateChatFeedback([FromBody] UpdateChatFeedbackRequest request)
        {
            var response = await _chatFeedbackBusiness.UpdateChatFeedbackAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data });
        }
    }
}
