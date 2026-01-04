using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications
{
    public class ChatFeedbacksByResponseIdsSpec : TenancySpecification<ChatFeedback>
    {
        public ChatFeedbacksByResponseIdsSpec(List<int> ids, int tenantId): base(tenantId)
        {
            Query.Where(m => ids.Contains(m.ResponseId));
        }
    }
}
