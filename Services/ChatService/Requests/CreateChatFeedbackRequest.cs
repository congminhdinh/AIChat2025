using Infrastructure;

namespace ChatService.Requests
{
    public class CreateChatFeedbackRequest: BaseRequest
    {
        public int MessageId { get; set; }
        public string Content { get; set; }
        public ChatFeedbackCategory Category { get; set; }
    }
}
