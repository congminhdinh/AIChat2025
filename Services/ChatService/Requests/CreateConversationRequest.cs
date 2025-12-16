using System.ComponentModel.DataAnnotations;

namespace ChatService.Requests;

public class CreateConversationRequest
{
    [Required]
    [MaxLength(500)]
    public string Title { get; init; } = string.Empty;

}
