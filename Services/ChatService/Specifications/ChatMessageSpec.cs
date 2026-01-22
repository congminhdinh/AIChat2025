using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications
{
    public class ChatMessageSpec: TenancySpecification<ChatMessage>
    {
        public ChatMessageSpec(int tenantId) : base(tenantId)
        {
            Query.AsNoTracking();
        }
    }
}
