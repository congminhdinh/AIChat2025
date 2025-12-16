namespace ChatService.Events;

/// <summary>
/// Event published to RabbitMQ when a user sends a message.
/// Consumed by Python ChatProcessor service.
/// Queue Name: UserPromptReceived
/// </summary>
public record UserPromptReceivedEvent
{
    public int ConversationId { get; init; }
    public string Message { get; init; } = string.Empty;
    public int UserId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
