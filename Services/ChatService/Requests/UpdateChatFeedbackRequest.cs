using Infrastructure;

namespace ChatService.Requests
{
    public class UpdateChatFeedbackRequest: BaseRequest
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public ChatFeedbackCategory Category { get; set; }
    }
}
