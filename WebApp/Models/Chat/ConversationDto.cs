namespace WebApp.Models.Chat
{
    public class ConversationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
