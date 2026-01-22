using Infrastructure;
using Infrastructure.Paging;

namespace ChatService.Requests
{
    public class GetListSystemPromptRequest : PaginatedRequest
    {
        public string? Name { get; set; }
        public int IsActive { get; set; } // -1: initialized, 0: inactive, 1: active
    }

    public class GetSystemPromptByIdRequest : BaseRequest
    {
        public int Id { get; set; }
    }
    public class CreateSystemPromptRequest : BaseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateSystemPromptRequest : CreateSystemPromptRequest
    {
        public int Id { get; set; }
    }

    public class DeleteSystemPromptRequest : BaseRequest
    {
        public int Id { get; set; }
    }

    public class SetActiveSystemPromptRequest : BaseRequest
    {
        public int Id { get; set; }
    }
}
