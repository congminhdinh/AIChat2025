using System.ComponentModel.DataAnnotations;

namespace ChatService.Requests;

public class SendMessageRequest
{
    [Required]
    public int ConversationId { get; init; }

    [Required]
    [MinLength(1)]
    public string Message { get; init; } = string.Empty;
}
