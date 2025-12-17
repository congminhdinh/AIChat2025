using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Specifications
{
    public class TenancySpecification<T>: Specification<T> where T : class
    {
        public TenancySpecification(int tenantId)
        {
            Query.AsNoTracking();
            if (tenantId != 1)
            {
                Query.Where(e => EF.Property<int>(e, "TenantId") == tenantId);
            }
        }
    }
}
