using Infrastructure.Paging;
using DocumentService.Enums;

namespace DocumentService.Requests
{
    public class GetDocumentListRequest : PaginatedRequest
    {
        public string? FileName { get; set; }
        public string? UploadedBy { get; set; }
        public DocumentAction? Action { get; set; }
        public bool? IsApproved { get; set; }
    }
}
