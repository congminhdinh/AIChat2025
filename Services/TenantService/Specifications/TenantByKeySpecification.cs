using Ardalis.Specification;
using TenantService.Entities;

namespace TenantService.Specifications
{
    public class TenantByKeySpecification: Specification<Tenant>
    {
        public TenantByKeySpecification(string tenantKey)
        {
            Query.Where(m => m.TenantKey == tenantKey && !m.IsDeleted && m.IsActive);
        }
    }
}
