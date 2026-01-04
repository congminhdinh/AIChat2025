using Infrastructure;

namespace ChatService.Requests
{
    public class CreateChatFeedbackRequest: BaseRequest
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
    }
}
