using DocumentService.Enums;

namespace DocumentService.Dtos
{
    public class DocumentChunkDto
    {
        public string? DocumentName { get; set; } = null;
        public DocType DocumentType { get; set; } = DocType.Initial;
        public string? FatherDocumentName { get; set; } = null;
        public string Heading1 { get; set; } = string.Empty;
        public string? Heading2 { get; set; } = null;
        public string Content { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
    }
}
