using Infrastructure.Paging;

namespace TenantService.Requests
{
    public class GetTenantListRequest: PaginatedRequest
    {
        public string? Name { get; set; }
    }
}
