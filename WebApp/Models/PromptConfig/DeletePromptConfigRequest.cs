using Infrastructure;

namespace WebApp.Models.PromptConfig
{
    public class DeletePromptConfigRequest : BaseRequest
    {
        public int Id { get; set; }
    }
}
