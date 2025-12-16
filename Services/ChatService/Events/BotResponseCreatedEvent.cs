namespace ChatService.Events;

/// <summary>
/// Event consumed from RabbitMQ when Python service generates a bot response.
/// Published by Python ChatProcessor service.
/// Queue Name: BotResponseCreated
/// </summary>
public record BotResponseCreatedEvent
{
    public int ConversationId { get; init; }
    public string Message { get; init; } = string.Empty;
    public int UserId { get; init; }
    public DateTime Timestamp { get; init; }
    public string? ModelUsed { get; init; }
}
