using Infrastructure;
using Infrastructure.Paging;

namespace AdminCMS.Models.Tenant
{
    public class GetTenantListRequest : PaginatedRequest
    {
        public string? Name { get; set; }
    }
}
