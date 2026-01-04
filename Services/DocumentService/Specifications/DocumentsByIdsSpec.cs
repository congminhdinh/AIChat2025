using Ardalis.Specification;
using DocumentService.Entities;
using Infrastructure.Specifications;

namespace DocumentService.Specifications
{
    public class DocumentsByIdsSpec : TenancySpecification<PromptDocument>
    {
        public DocumentsByIdsSpec(List<int> ids, int tenantId): base(tenantId)
        {
            Query.Where(d => ids.Contains(d.Id));
        }
    }
}
