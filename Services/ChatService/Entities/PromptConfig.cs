using Infrastructure.Entities;

namespace ChatService.Entities
{
    public class PromptConfig : TenancyEntity
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
