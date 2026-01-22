using ChatService.Enums;
using Infrastructure.Dtos;

namespace ChatService.Dtos;

public class MessageDto
{
    public int Id { get; init; }
    public int ConversationId { get; init; }
    public int RequestId { get; init; } = 0;
    public List<DocumentChatDto> ReferenceDocList { get; init; } = new List<DocumentChatDto>();
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool IsBot { get; init; }
    public int UserId { get; init; }
    public int FeedbackId { get; init; } = 0;
    public short Ratings { get; init; } = 0;
    public ChatType Type { get; init; }
}
