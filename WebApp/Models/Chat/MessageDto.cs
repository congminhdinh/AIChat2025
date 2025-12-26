namespace WebApp.Models.Chat
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsBot { get; set; }
        public int UserId { get; set; }
        public int Type { get; set; }
    }
}
