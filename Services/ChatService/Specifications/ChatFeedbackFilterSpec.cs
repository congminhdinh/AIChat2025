using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications
{
    public class ChatFeedbackFilterSpec: TenancySpecification<ChatFeedback>
    {
        public ChatFeedbackFilterSpec(int tenantId, short rating, int pageIndex, int pageSize): base(tenantId)
        {
            Query.Where(m => m.Ratings == rating).OrderByDescending(m => m.LastModifiedAt).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
        }

        public ChatFeedbackFilterSpec(int tenantId, short rating) : base(tenantId)
        {
            Query.Where(m => m.Ratings == rating);
        }
    }

    public class ChatFeedbackByMessageSpec : TenancySpecification<ChatFeedback>
    {
        public ChatFeedbackByMessageSpec(int tenantId, int messageId): base(tenantId)
        {
            Query.Where(m => m.MessageId == messageId);
        }
    }
}
