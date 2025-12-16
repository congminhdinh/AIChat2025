using Infrastructure.Entities;

namespace ChatService.Entities
{
    public class ChatMessage : TenancyEntity
    {
        public ChatMessage()
        {
        }

        public ChatMessage(int conversationId, string message, int userId)
        {
            ConversationId = conversationId;
            Message = message;
            UserId = userId;
        }

        public int ConversationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ChatConversation? Conversation { get; set; }
    }
}
