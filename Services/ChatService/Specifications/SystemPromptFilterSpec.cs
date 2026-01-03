using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications;

/// <summary>
/// Specification for filtering SystemPrompts by name and active status.
/// </summary>
public sealed class SystemPromptFilterSpec : TenancySpecification<SystemPrompt>
{
    public SystemPromptFilterSpec(string? name, bool? isActive, int tenantId) : base(tenantId)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Query.Where(x => x.Name.Contains(name));
        }

        if (isActive.HasValue)
        {
            Query.Where(x => x.IsActive == isActive.Value);
        }

        Query.OrderByDescending(x => x.IsActive)
             .ThenByDescending(x => x.LastModifiedAt);
    }
}
