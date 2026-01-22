namespace ChatService.Events;

public record BotResponseCreatedEvent
{
    public int ConversationId { get; init; }
    public int RequestId { get; init; }
    public List<int> ReferenceDocIdList { get; init; } = new List<int>();
    public string Message { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string? ModelUsed { get; init; }
}
