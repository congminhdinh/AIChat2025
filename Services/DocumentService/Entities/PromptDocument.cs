using DocumentService.Enums;
using Infrastructure.Entities;

namespace DocumentService.Entities
{
    public class PromptDocument: TenancyEntity
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? DocumentName { get; set; }
        public DocumentAction Action { get; set; }
        public DocType DocumentType { get; set; } = DocType.Initial;
        public int FatherDocumentId { get; set; } = -1;
        public string? UploadedBy { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public string? ApprovedBy { get; set; }

    }
}
