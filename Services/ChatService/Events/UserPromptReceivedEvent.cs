namespace ChatService.Events;

public record UserPromptReceivedEvent
{
    public int ConversationId { get; init; }
    public string Message { get; init; } = string.Empty;
    public int UserId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
