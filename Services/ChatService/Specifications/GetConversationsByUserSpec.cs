using Ardalis.Specification;
using ChatService.Entities;

namespace ChatService.Features;

/// <summary>
/// Specification to get conversations for a specific user, ordered by last message time.
/// </summary>
public sealed class GetConversationsByUserSpec : Specification<ChatConversation>
{
    public GetConversationsByUserSpec(int userId)
    {
        Query
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .Include(c => c.Messages);
    }
}
