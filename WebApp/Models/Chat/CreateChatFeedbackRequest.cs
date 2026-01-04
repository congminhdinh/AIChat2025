namespace WebApp.Models.Chat
{
    public class CreateChatFeedbackRequest
    {
        public int MessageId { get; set; }
        public string Content { get; set; } = string.Empty;
        public ChatFeedbackCategory Category { get; set; }
    }
}
