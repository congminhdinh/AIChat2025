using Infrastructure.Paging;

namespace ChatService.Requests
{
    public class GetChatFeedbackListRequest: PaginatedRequest
    {
        public short Ratings { get; set; } = 0;
    }
}
