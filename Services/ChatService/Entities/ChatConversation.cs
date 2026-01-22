using Infrastructure.Entities;

namespace ChatService.Entities
{
    public class ChatConversation : TenancyEntity
    {
        public ChatConversation()
        {
        }

        public ChatConversation(int userId, string title)
        {
            UserId = userId;
            Title = title;
        }

        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
