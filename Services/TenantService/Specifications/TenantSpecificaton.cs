using Ardalis.Specification;
using TenantService.Entities;

namespace TenantService.Specifications
{
    public class TenantListSpec: Specification<Tenant>
    {
        public TenantListSpec(string? name, int pageIndex, int pageSize)
        {
            Query.Where(m => !m.IsDeleted &&
            (string.IsNullOrEmpty(name) || m.Name.Contains(name))).OrderByDescending(m => m.Id).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
        }
    }
    public class TenantSpecification
    {
        
    }
}
