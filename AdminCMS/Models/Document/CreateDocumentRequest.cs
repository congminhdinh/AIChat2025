using AdminCMS.Models.Document.Enums;
using Infrastructure;

namespace AdminCMS.Models.Document
{
    public class CreateDocumentRequest : BaseRequest
    {
        public IFormFile File { get; set; } = null!;
        public DocType DocumentType { get; set; } = 0;
        public int FatherDocumentId { get; set; } = -1;
        public string? DocumentName { get; set; }
    }
}
