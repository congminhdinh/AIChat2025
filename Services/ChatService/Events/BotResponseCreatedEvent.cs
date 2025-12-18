namespace ChatService.Events;

public record BotResponseCreatedEvent
{
    public int ConversationId { get; init; }
    public string Message { get; init; } = string.Empty;
    public int UserId { get; init; }
    public int TenantId { get; init; }
    public DateTime Timestamp { get; init; }
    public string? ModelUsed { get; init; }
}
