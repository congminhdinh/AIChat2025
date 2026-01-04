using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications
{
    public class ChatFeedbackFilterSpec: TenancySpecification<ChatFeedback>
    {
        public ChatFeedbackFilterSpec(int tenantId, short rating, int pageIndex, int pageSize): base(tenantId)
        {
            
        }
    }
}
