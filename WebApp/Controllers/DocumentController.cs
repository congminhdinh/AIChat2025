using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Business;
using WebApp.Helpers;
using WebApp.Models.Document;

namespace WebApp.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly DocumentBusiness _documentBusiness;
        private readonly IdentityHelper _identityHelper;

        public DocumentController(DocumentBusiness documentBusiness, IdentityHelper identityHelper)
        {
            _documentBusiness = documentBusiness;
            _identityHelper = identityHelper;
        }

        public IActionResult Index()
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            var request = new GetDocumentListRequest
            {
                FileName = keyword,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            var response = await _documentBusiness.GetListAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return PartialView("GetListDocumentPartial", new WebApp.Models.PaginatedListDto<DocumentDto>
                {
                    Items = new List<DocumentDto>(),
                    PageIndex = 1,
                    TotalPages = 0,
                    PageSize = 0
                });
            }

            return PartialView("GetListDocumentPartial", response.Data);
        }

        [HttpGet]
        public IActionResult UploadDocumentPartial()
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            return PartialView();
        }

        [HttpGet]
        public async Task<IActionResult> GetListDocumentPartial([FromQuery] GetDocumentListRequest request)
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            var response = await _documentBusiness.GetListAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetDocument(int id)
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            var response = await _documentBusiness.GetByIdAsync(id);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data });
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? documentName)
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn tệp để tải lên" });
            }

            var request = new CreateDocumentRequest
            {
                File = file,
                DocumentType = Models.Document.Enums.DocType.Initial,
                FatherDocumentId = -1,
                DocumentName = documentName
            };

            var response = await _documentBusiness.UploadAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data, message = "Tải lên tài liệu thành công" });
        }
        [HttpGet]
        public async Task<IActionResult> GetDocumentById(int id)
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            var response = await _documentBusiness.GetByIdAsync(id);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            ViewBag.DocumentId = id;
            ViewBag.DocumentName = response.Data.FileName;

            return PartialView("GetDocumentDetail");
        }

        public IActionResult GetDocumentDetail()
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] UpdateDocumentRequest request)
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            if (request.DocumentId <= 0)
            {
                return Json(new { success = false, message = "ID tài liệu không hợp lệ" });
            }

            // ENFORCE HARDCODED VALUES AS PER REQUIREMENTS
            request.DocType = Models.Document.Enums.DocType.Initial;
            request.FatherDocumentId = -1;

            var response = await _documentBusiness.UpdateAsync(request);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, data = response.Data, message = "Cập nhật tài liệu thành công" });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            if (id <= 0)
            {
                return Json(new { success = false, message = "ID tài liệu không hợp lệ" });
            }

            var response = await _documentBusiness.DeleteAsync(id);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, message = "Xóa tài liệu thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> Vectorize(int id)
        {
            if (!_identityHelper.IsAdmin())
            {
                return View("AccessDenied");
            }
            if (id <= 0)
            {
                return Json(new { success = false, message = "ID tài liệu không hợp lệ" });
            }

            var response = await _documentBusiness.VectorizeAsync(id);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, message = "Đã gửi yêu cầu nạp dữ liệu cho tài liệu" });
        }
    }
}
