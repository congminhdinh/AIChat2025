namespace WebApp.Models.Chat
{
    public class MessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int Type { get; set; } // 0 = User Request, 1 = Bot Response
    }
}
