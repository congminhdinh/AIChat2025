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
            return View();
        }

        [HttpGet]
        public IActionResult UploadDocumentPartial()
        {
            return PartialView();
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] GetDocumentListRequest request)
        {
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

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] UpdateDocumentRequest request)
        {
            if (request.DocumentId <= 0)
            {
                return Json(new { success = false, message = "ID tài liệu không hợp lệ" });
            }

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
            if (id <= 0)
            {
                return Json(new { success = false, message = "ID tài liệu không hợp lệ" });
            }

            var response = await _documentBusiness.VectorizeAsync(id);

            if (response.Status == BaseResponseStatus.Error)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new { success = true, message = "Đã gửi yêu cầu vectorize tài liệu" });
        }
    }
}
