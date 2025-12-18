using Infrastructure;
using Infrastructure.Paging;

namespace ChatService.Requests
{
    public class GetListPromptConfiRequest: PaginatedRequest
    {
        public string? Key { get; set; }
    }

    public class CreatePromptConfigRequest: BaseRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class UpdatePromptConfigRequest: CreatePromptConfigRequest
    {
        public int Id { get; set; }
    }

    public class  DeletePromptConfigRequest: BaseRequest
    {
        public int Id { get; set; }
    }
}
