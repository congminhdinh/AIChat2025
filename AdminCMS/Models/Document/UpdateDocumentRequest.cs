using AdminCMS.Models.Document.Enums;
using Infrastructure;

namespace AdminCMS.Models.Document
{
    public class UpdateDocumentRequest : BaseRequest
    {
        public int DocumentId { get; set; }
        public string? DocumentName { get; set; }
        public DocType DocType { get; set; }
        public int FatherDocumentId { get; set; } = -1;
    }
}
