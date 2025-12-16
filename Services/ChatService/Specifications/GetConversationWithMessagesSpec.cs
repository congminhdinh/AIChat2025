using Ardalis.Specification;
using ChatService.Entities;

namespace ChatService.Features;

/// <summary>
/// Specification to get a single conversation with all its messages.
/// </summary>
public sealed class GetConversationWithMessagesSpec : Specification<ChatConversation>, ISingleResultSpecification<ChatConversation>
{
    public GetConversationWithMessagesSpec(int conversationId)
    {
        Query
            .Where(c => c.Id == conversationId)
            .Include(c => c.Messages);
    }
}
