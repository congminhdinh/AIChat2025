namespace WebApp.Models.Chat
{
    public class UpdateChatFeedbackRequest
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public ChatFeedbackCategory Category { get; set; }
    }
}
