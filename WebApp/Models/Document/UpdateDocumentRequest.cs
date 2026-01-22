using Infrastructure;
using WebApp.Models.Document.Enums;

namespace WebApp.Models.Document
{
    public class UpdateDocumentRequest : BaseRequest
    {
        public int DocumentId { get; set; }
        public string? DocumentName { get; set; }
        public DocType DocType { get; set; }
        public int FatherDocumentId { get; set; } = -1;
    }
}
