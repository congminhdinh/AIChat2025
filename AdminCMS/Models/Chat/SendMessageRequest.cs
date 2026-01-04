namespace AdminCMS.Models.Chat
{
    public class SendMessageRequest
    {
        public int ConversationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
