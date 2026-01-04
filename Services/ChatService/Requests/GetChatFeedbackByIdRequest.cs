using Infrastructure;

namespace ChatService.Requests
{
    public class GetChatFeedbackByIdRequest : BaseRequest
    {
        public int Id { get; init; }
    }
}
