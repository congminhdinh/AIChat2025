using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications
{
    public class ChatMessagesByIdsSpec : TenancySpecification<ChatMessage>
    {
        public ChatMessagesByIdsSpec(List<int> ids, int tenantId): base(tenantId)
        {
            Query.Where(m => ids.Contains(m.Id));
        }
    }
}
