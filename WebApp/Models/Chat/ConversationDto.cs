namespace WebApp.Models.Chat
{
    public class ConversationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public int MessageCount { get; set; }
        public List<MessageDto> Messages { get; set; } = new();
    }
}
