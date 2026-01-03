using WebApp.Models.Document.Enums;

namespace WebApp.Models.Document
{
    public class DocumentDto
    {
        public DocumentDto(
            int id,
            string fileName,
            string filePath,
            DocumentAction action,
            bool isApproved,
            string? uploadedBy,
            string? approvedBy,
            DateTime createdAt,
            DateTime? lastModifiedAt)
        {
            Id = id;
            FileName = fileName;
            FilePath = filePath;
            Action = action;
            IsApproved = isApproved;
            UploadedBy = uploadedBy;
            ApprovedBy = approvedBy;
            CreatedAt = createdAt;
            LastModifiedAt = lastModifiedAt;
        }

        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DocumentAction Action { get; set; }
        public bool IsApproved { get; set; }
        public string? UploadedBy { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public int TenantId { get; set; }
    }
}
