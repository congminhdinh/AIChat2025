using AdminCMS.Business;
using AdminCMS.Helpers;
using AdminCMS.Models;
using AdminCMS.Models.Tenant;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AdminCMS.Controllers
{
    [Authorize]
    public class TenantController : Controller
    {
        private readonly TenantBusiness _tenantBusiness;
        private readonly IdentityHelper _identityHelper;
        private readonly AppSettings _appSettings;

        public TenantController(TenantBusiness tenantBusiness, IdentityHelper identityHelper, IOptions<AppSettings> appSettings)
        {
            _tenantBusiness = tenantBusiness;
            _identityHelper = identityHelper;
            _appSettings = appSettings.Value;
        }

        public IActionResult Index()
        {
            // Global [Authorize] filter handles authentication
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTenants([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var request = new GetTenantListRequest
            {
                Name = keyword,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            var response = await _tenantBusiness.GetListAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return PartialView("_TenantList", new PaginatedListDto<TenantDto>
                {
                    Items = new List<TenantDto>(),
                    PageIndex = 1,
                    TotalPages = 0,
                    PageSize = 0
                });
            }

            return PartialView("_TenantList", response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetTenantById(int id)
        {
            var response = await _tenantBusiness.GetByIdAsync(id);

            if (response.Status == BaseResponseStatus.Error || response.Data == null)
            {
                return Json(new { success = false, message = response.Message ?? "Không tìm thấy tenant" });
            }

            var viewModel = new TenantDetailViewModel
            {
                TenantId = id,
                TenantName = response.Data.Name ?? string.Empty,
                Description = response.Data.Description,
                IsActive = response.Data.IsActive
            };

            return PartialView("GetTenantDetail", viewModel);
        }

        [HttpGet]
        public IActionResult GetCreateTenantModal()
        {
            return PartialView("_CreateTenantPartial");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateTenantRequest request)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Json(new { success = false, message = "Vui lòng nhập tên tenant" });
            }

            var response = await _tenantBusiness.CreateAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data, message = "Tạo tenant thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromForm] UpdateTenantRequest request)
        {
            if (request.Id <= 0)
            {
                return Json(new { success = false, message = "ID tenant không hợp lệ" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Json(new { success = false, message = "Vui lòng nhập tên tenant" });
            }

            var response = await _tenantBusiness.UpdateAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data, message = "Cập nhật tenant thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> Deactivate(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "ID tenant không hợp lệ" });
            }

            var response = await _tenantBusiness.DeactivateAsync(id);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, message = "Vô hiệu hóa tenant thành công" });
        }

        [HttpGet]
        public async Task<IActionResult> GetTenantKey(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "ID tenant không hợp lệ" });
            }

            var response = await _tenantBusiness.GetTenantKeyAsync(id);

            if (response.Status == BaseResponseStatus.Error || response.Data == null)
            {
                return Json(new { success = false, message = response.Message ?? "Không tìm thấy tenant key" });
            }

            var viewModel = new TenantKeyViewModel
            {
                TenantId = response.Data.Id,
                TenantKey = response.Data.TenantKey ?? string.Empty
            };

            return PartialView("_TenantKeyPartial", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> RefreshTenantKey(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "ID tenant không hợp lệ" });
            }

            var response = await _tenantBusiness.RefreshTenantKeyAsync(id);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, message = "Làm mới tenant key thành công" });
        }
    }
}
