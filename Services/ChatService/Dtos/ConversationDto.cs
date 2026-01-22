namespace ChatService.Dtos;

public class ConversationDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastMessageAt { get; init; }
    public int MessageCount { get; init; }
    public List<MessageDto> Messages { get; init; } = new();
}
