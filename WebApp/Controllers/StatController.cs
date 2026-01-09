using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using WebApp.Business;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Models.Chat;

namespace WebApp.Controllers
{
    public class StatController: Controller
    {
        private readonly ChatBusiness _chatBusiness;
        private readonly ChatFeedbackBusiness _chatFeedbackBusiness;
        private readonly IdentityHelper _identityHelper;

        public StatController(
            ChatBusiness chatBusiness,
            ChatFeedbackBusiness chatFeedbackBusiness,
            IdentityHelper identityHelper)
        {
            _chatBusiness = chatBusiness;
            _chatFeedbackBusiness = chatFeedbackBusiness;
            _identityHelper = identityHelper;
        }

        public ActionResult Index()
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMessageCount()
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            var response = await _chatBusiness.CountMessage();

            if (response.Status == BaseResponseStatus.Error)
                return Json(new { success = false, message = response.Message });

            return Json(new { success = true, data = response.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetRatingCounts()
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            var response = await _chatFeedbackBusiness.GetChatFeedbackListAsync(0, 1, 1000);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = "Không thể tải số liệu đánh giá" });
            }
            var likesCount = response.Data?.Items.Count(m => m.Ratings == 1) ?? 0;
            var dislikesCount = response.Data?.Items.Count(m => m.Ratings == 2) ?? 0;

            return Json(new {
                success = true,
                data = new {
                    Likes = likesCount,
                    Dislikes = dislikesCount,
                    Total = response.Data?.Items.Count ?? 0
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetFeedbackList(
            [FromQuery] short? ratings = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            var response = await _chatFeedbackBusiness.GetChatFeedbackListAsync(ratings, pageIndex, pageSize);

            if (response.Status == BaseResponseStatus.Error || response.Data == null)
            {
                return PartialView("_FeedbackList", new PaginatedListDto<ChatFeedbackDto>
                {
                    Items = new List<ChatFeedbackDto>(),
                    PageIndex = 1,
                    TotalPages = 0,
                    PageSize = pageSize
                });
            }

            return PartialView("_FeedbackList", response.Data);
        }
    }
}
