using Infrastructure;

namespace ChatService.Requests
{
    public class RateChatFeedbackRequest: BaseRequest
    {
        public int Id { get; set; }
        public short Ratings { get; set; }
    }
}
