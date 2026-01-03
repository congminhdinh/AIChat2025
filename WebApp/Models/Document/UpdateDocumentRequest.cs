using Infrastructure;
using WebApp.Models.Document.Enums;

namespace WebApp.Models.Document
{
    public class UpdateDocumentRequest : BaseRequest
    {
        public int DocumentId { get; set; }
        public bool IsApproved { get; set; }
        public string? ApprovedBy { get; set; }
        public DocumentAction? Action { get; set; }
    }
}
