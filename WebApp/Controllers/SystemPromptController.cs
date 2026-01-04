using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Business;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Models.SystemPrompt;

namespace WebApp.Controllers
{
    [Authorize]
    public class SystemPromptController : Controller
    {
        private readonly SystemPromptBusiness _systemPromptBusiness;
        private readonly IdentityHelper _identityHelper;

        public SystemPromptController(SystemPromptBusiness systemPromptBusiness, IdentityHelper identityHelper)
        {
            _systemPromptBusiness = systemPromptBusiness;
            _identityHelper = identityHelper;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSystemPrompts([FromQuery] string? keyword, [FromQuery] int isActive = -1, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var request = new GetListSystemPromptRequest
            {
                Name = keyword,
                IsActive = isActive,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            var response = await _systemPromptBusiness.GetListAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return PartialView("_SystemPromptList", new PaginatedListDto<SystemPromptDto>
                {
                    Items = new List<SystemPromptDto>(),
                    PageIndex = 1,
                    TotalPages = 0,
                    PageSize = 0
                });
            }

            return PartialView("_SystemPromptList", response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetSystemPromptById(int id)
        {
            var response = await _systemPromptBusiness.GetByIdAsync(id);

            if (response.Status == BaseResponseStatus.Error || response.Data == null)
            {
                return Json(new { success = false, message = response.Message ?? "Không tìm thấy system prompt" });
            }

            return PartialView("GetSystemPromptDetail", response.Data);
        }

        [HttpGet]
        public IActionResult GetCreateSystemPromptModal()
        {
            return PartialView("_CreateSystemPromptPartial");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSystemPromptRequest request)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Json(new { success = false, message = "Vui lòng nhập tên" });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung" });
            }

            var response = await _systemPromptBusiness.CreateAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data, message = "Tạo system prompt thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateSystemPromptRequest request)
        {
            if (request.Id <= 0)
            {
                return Json(new { success = false, message = "ID không hợp lệ" });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Json(new { success = false, message = "Vui lòng nhập tên" });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung" });
            }

            var response = await _systemPromptBusiness.UpdateAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data, message = "Cập nhật system prompt thành công" });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "ID không hợp lệ" });
            }

            var response = await _systemPromptBusiness.DeleteAsync(id);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, message = "Xóa system prompt thành công" });
        }
    }
}
