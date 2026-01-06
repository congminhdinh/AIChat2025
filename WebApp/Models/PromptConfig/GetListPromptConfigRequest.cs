using Infrastructure.Paging;

namespace WebApp.Models.PromptConfig
{
    public class GetListPromptConfiRequest : PaginatedRequest
    {
        public string? Key { get; set; }
    }
}
