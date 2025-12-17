using ChatService.Enums;

namespace ChatService.Dtos;

public class MessageDto
{
    public int Id { get; init; }
    public int ConversationId { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool IsBot { get; init; }
    public int UserId { get; init; }
    public ChatType Type { get; init; }
}
