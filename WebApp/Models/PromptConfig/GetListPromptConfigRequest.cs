using Infrastructure.Paging;

namespace WebApp.Models.PromptConfig
{
    public class GetListPromptConfigRequest : PaginatedRequest
    {
        public string? Key { get; set; }
    }
}
