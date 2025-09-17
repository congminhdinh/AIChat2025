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
}
