using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Business;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Models.PromptConfig;

namespace WebApp.Controllers
{
    [Authorize]
    public class PromptConfigController : Controller
    {
        private readonly PromptConfigBusiness _systemPromptBusiness;
        private readonly IdentityHelper _identityHelper;

        public PromptConfigController(PromptConfigBusiness systemPromptBusiness, IdentityHelper identityHelper)
        {
            _systemPromptBusiness = systemPromptBusiness;
            _identityHelper = identityHelper;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPromptConfigs([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var request = new GetListPromptConfigRequest
            {
                Key = keyword,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            var response = await _systemPromptBusiness.GetListAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return PartialView("_PromptConfigList", new PaginatedListDto<PromptConfigDto>
                {
                    Items = new List<PromptConfigDto>(),
                    PageIndex = 1,
                    TotalPages = 0,
                    PageSize = 0
                });
            }

            return PartialView("_PromptConfigList", response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetPromptConfigById(int id)
        {
            var response = await _systemPromptBusiness.GetByIdAsync(id);

            if (response.Status == BaseResponseStatus.Error || response.Data == null)
            {
                return Json(new { success = false, message = response.Message ?? "Không tìm thấy system prompt" });
            }

            return PartialView("GetPromptConfigDetail", response.Data);
        }

        [HttpGet]
        public IActionResult GetCreatePromptConfigModal()
        {
            return PartialView("_CreatePromptConfigPartial");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePromptConfigRequest request)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return Json(new { success = false, message = "Vui lòng nhập key" });
            }

            if (string.IsNullOrWhiteSpace(request.Value))
            {
                return Json(new { success = false, message = "Vui lòng nhập giá trị" });
            }

            var response = await _systemPromptBusiness.CreateAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data, message = "Tạo system prompt thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdatePromptConfigRequest request)
        {
            if (request.Id <= 0)
            {
                return Json(new { success = false, message = "ID không hợp lệ" });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return Json(new { success = false, message = "Vui lòng nhập key" });
            }

            if (string.IsNullOrWhiteSpace(request.Value))
            {
                return Json(new { success = false, message = "Vui lòng nhập giá trị" });
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
