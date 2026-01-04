using ChatService.Enums;

namespace ChatService.Dtos;

public class MessageDto
{
    public int Id { get; init; }
    public int ConversationId { get; init; }
    public int RequestId { get; init; } = 0;
    public List<int> ReferenceDocIdList { get; init; } = new List<int>();
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool IsBot { get; init; }
    public int UserId { get; init; }
    public ChatType Type { get; init; }
}
