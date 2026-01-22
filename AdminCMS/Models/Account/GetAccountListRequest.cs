using Infrastructure.Paging;

namespace AdminCMS.Models.Account
{
    public class GetAccountListRequest : PaginatedRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
}
