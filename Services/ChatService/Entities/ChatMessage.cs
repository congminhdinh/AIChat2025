using ChatService.Enums;
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
        public ChatType Type { get; set; }
        public int RequestId { get; set; } = 0;
        public string ReferenceDocIds { get; set; } = "";
        public virtual ChatConversation? Conversation { get; set; }
    }
}
