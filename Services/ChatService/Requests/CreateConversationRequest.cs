using Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace ChatService.Requests;

public class CreateConversationRequest: BaseRequest
{
    [Required]
    [MaxLength(500)]
    public string Title { get; init; } = string.Empty;

}
