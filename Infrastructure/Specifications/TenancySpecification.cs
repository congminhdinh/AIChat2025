using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Specifications
{
    public class TenancySpecification<T>: Specification<T> where T : class
    {
        public TenancySpecification(int tenantId)
        {
            Query.AsNoTracking();
            Query.Where(e => EF.Property<int>(e, "TenantId") == tenantId);
        }
    }
}
