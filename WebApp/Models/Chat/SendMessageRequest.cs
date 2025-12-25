namespace WebApp.Models.Chat
{
    public class SendMessageRequest
    {
        public int ConversationId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
