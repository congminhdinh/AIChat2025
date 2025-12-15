using Infrastructure.Paging;

namespace AccountService.Requests
{
    public class GetAccountListRequest : PaginatedRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
}
