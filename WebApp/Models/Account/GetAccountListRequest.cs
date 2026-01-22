using Infrastructure.Paging;

namespace WebApp.Models.Account
{
    public class GetAccountListRequest : PaginatedRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
}
