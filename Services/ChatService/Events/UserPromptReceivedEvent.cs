using ChatService.Dtos;

namespace ChatService.Events;

public record UserPromptReceivedEvent
{
    public int ConversationId { get; init; }
    public int MessageId { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public List<PromptConfigDto> SystemInstruction { get; init; } = new();
    public string? SystemPrompt { get; init; }
}
