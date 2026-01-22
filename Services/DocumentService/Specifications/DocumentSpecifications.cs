using Ardalis.Specification;
using DocumentService.Entities;
using DocumentService.Enums;
using Infrastructure.Specifications;

namespace DocumentService.Specifications
{
    public class DocumentSpecificationById : TenancySpecification<PromptDocument>
    {
        public DocumentSpecificationById(int documentId, int tenantId) : base(tenantId)
        {
            Query.Where(d => d.Id == documentId && !d.IsDeleted);
        }
    }

    public class DocumentListSpec : TenancySpecification<PromptDocument>
    {
        // For count queries
        public DocumentListSpec(
            int tenantId,
            string? fileName,
            string? uploadedBy,
            DocumentAction? action,
            bool? isApproved) : base(tenantId)
        {
            ApplyFilters(fileName, uploadedBy, action, isApproved);
        }

        // For paginated queries
        public DocumentListSpec(
            int tenantId,
            string? fileName,
            string? uploadedBy,
            DocumentAction? action,
            bool? isApproved,
            int pageIndex,
            int pageSize) : base(tenantId)
        {
            ApplyFilters(fileName, uploadedBy, action, isApproved);
            Query.OrderByDescending(d => d.CreatedAt)
                 .Skip(pageSize * (pageIndex - 1))
                 .Take(pageSize);
        }

        private void ApplyFilters(
            string? fileName,
            string? uploadedBy,
            DocumentAction? action,
            bool? isApproved)
        {
            Query.Where(d => !d.IsDeleted &&
                (string.IsNullOrEmpty(fileName) || d.FileName.Contains(fileName)) &&
                (string.IsNullOrEmpty(uploadedBy) || d.UploadedBy == uploadedBy) &&
                (action == null || d.Action == action) &&
                (isApproved == null || d.IsApproved == isApproved)
            );
        }
    }
}
