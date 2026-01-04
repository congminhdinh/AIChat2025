using Infrastructure;

namespace ChatService.Requests
{
    public class RateChatFeedbackRequest: BaseRequest
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public short Ratings { get; set; }
    }
}
