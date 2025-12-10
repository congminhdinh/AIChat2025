using AccountService.Entities;
using Ardalis.Specification;
using Infrastructure.Specifications;

namespace AccountService.Specifications
{
    public class AccountSpecification: TenancySpecification<Account>
    {
        public AccountSpecification(string email, int tenantId) : base(tenantId)
        {
            Query.Where(m => m.Email == email);
        }
    }

    public class AccountSpecificationById : TenancySpecification<Account>
    {
        public AccountSpecificationById(int accountId, int tenantId) : base(tenantId)
        {
            Query.Where(m => m.Id == accountId);
        }
    }

    public class AccountListSpec : Specification<Account>
    {
        public AccountListSpec()
        {
            Query.OrderByDescending(m => m.Id);
        }
        public AccountListSpec(int tenantId, string? name, string? username, string? email, int pageIndex, int pageSize)
        {
            Query.Where(m => !m.IsDeleted && (m.TenantId == tenantId) &&
            (string.IsNullOrEmpty(name) || m.Name.Contains(name)) &&
            (string.IsNullOrEmpty(email) || m.Email.Contains(email))
            );
            Query.OrderByDescending(m => m.Id).Skip(pageSize*(pageIndex-1)).Take(pageSize);
        }
    }
}
