using Infrastructure;

namespace ChatService.Requests
{
    public class GetConversationByIdRequest : BaseRequest
    {
        public int ConversationId { get; init; }
    }
}
