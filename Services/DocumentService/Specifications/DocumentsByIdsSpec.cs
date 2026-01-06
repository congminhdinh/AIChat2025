using Ardalis.Specification;
using DocumentService.Entities;
using Infrastructure.Specifications;

namespace DocumentService.Specifications
{
    public class DocumentsByIdsSpec : Specification<PromptDocument>
    {
        public DocumentsByIdsSpec(List<int> ids)
        {
            Query.Where(d => ids.Contains(d.Id));
        }
    }
}
