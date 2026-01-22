namespace WebApp.Models.Chat
{
    public class ChatFeedbackDetailDto
    {
        public int Id { get; set; }
        public short Ratings { get; set; }
        public string Content { get; set; } = string.Empty;
        public ChatFeedbackCategory Category { get; set; }
    }
}
