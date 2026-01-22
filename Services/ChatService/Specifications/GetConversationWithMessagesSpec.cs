using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications;

/// <summary>
/// Specification to get a single conversation with all its messages.
/// </summary>
public sealed class GetConversationWithMessagesSpec : TenancySpecification<ChatConversation>, ISingleResultSpecification<ChatConversation>
{
    public GetConversationWithMessagesSpec(int conversationId, int tenantId): base(tenantId)
    {
        Query
            .Where(c => c.Id == conversationId)
            .Include(c => c.Messages).OrderByDescending(c => c.Id);
    }
}
