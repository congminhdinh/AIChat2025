using WebApp.Models.Document.Enums;

namespace WebApp.Models.Document
{
    public class DocumentDto
    {
        public DocumentDto(
            int id,
            string fileName,
            string? documentName,
            string filePath,
            DocumentAction action,
            DateTime? lastModifiedAt)
        {
            Id = id;
            FileName = fileName;
            DocumentName = documentName;
            FilePath = filePath;
            Action = action;
            LastModifiedAt = lastModifiedAt;
        }

        public int Id { get; set; }
        public string FileName { get; set; }
        public string? DocumentName { get; set; }
        public string FilePath { get; set; }
        public DocumentAction Action { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public int TenantId { get; set; }
    }
}
