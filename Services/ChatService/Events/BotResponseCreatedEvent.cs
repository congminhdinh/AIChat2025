namespace ChatService.Events;

public record BotResponseCreatedEvent
{
    public int ConversationId { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string? ModelUsed { get; init; }
}
