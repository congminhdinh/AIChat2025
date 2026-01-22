using Infrastructure;

namespace WebApp.Models.SystemPrompt
{
    public class GetSystemPromptByIdRequest : BaseRequest
    {
        public int Id { get; set; }
    }
}
