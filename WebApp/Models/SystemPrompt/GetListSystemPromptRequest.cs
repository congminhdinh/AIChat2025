using Infrastructure.Paging;

namespace WebApp.Models.SystemPrompt
{
    public class GetListSystemPromptRequest : PaginatedRequest
    {
        public string? Name { get; set; }
        public int IsActive { get; set; } = -1; // -1: all, 0: inactive, 1: active
    }
}
