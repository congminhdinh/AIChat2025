
using DocumentService.Enums;
using Infrastructure;

namespace DocumentService.Requests
{
    public class CreateDocumentRequest : BaseRequest
    {
        public IFormFile File { get; set; } = null!;
        public DocType DocumentType { get; set; } = DocType.Initial;
        public int FatherDocumentId { get; set; } = -1;
        public string? DocumentName { get; set; }
        }
}
