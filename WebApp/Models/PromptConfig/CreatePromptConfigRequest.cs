using Infrastructure;

namespace WebApp.Models.PromptConfig
{
    public class CreatePromptConfigRequest : BaseRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
