using Infrastructure.Entities;

namespace ChatService.Entities
{
    public class SystemPrompt : TenancyEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
