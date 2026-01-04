using Infrastructure;

namespace WebApp.Models.SystemPrompt
{
    public class DeleteSystemPromptRequest : BaseRequest
    {
        public int Id { get; set; }
    }
}
