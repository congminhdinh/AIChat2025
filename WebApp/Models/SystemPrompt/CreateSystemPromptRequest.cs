using Infrastructure;

namespace WebApp.Models.SystemPrompt
{
    public class CreateSystemPromptRequest : BaseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
