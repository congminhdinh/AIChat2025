using AdminCMS.Models.Document.Enums;
using Infrastructure.Paging;

namespace AdminCMS.Models.Document
{
    public class GetDocumentListRequest : PaginatedRequest
    {
        public string? FileName { get; set; }
        public string? UploadedBy { get; set; }
        public DocumentAction? Action { get; set; }
        public bool? IsApproved { get; set; }
    }
}
