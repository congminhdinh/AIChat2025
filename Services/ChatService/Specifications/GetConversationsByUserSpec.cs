using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications;

/// <summary>
/// Specification to get conversations for a specific user, ordered by last message time.
/// </summary>
public sealed class GetConversationsByUserSpec : TenancySpecification<ChatConversation>
{
    public GetConversationsByUserSpec(int userId, int tenantId): base(tenantId)
    {
        Query
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .Include(c => c.Messages);
    }
}
