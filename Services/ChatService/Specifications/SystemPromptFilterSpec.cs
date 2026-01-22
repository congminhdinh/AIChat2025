using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications;

/// <summary>
/// Specification for filtering SystemPrompts by name and active status.
/// </summary>
public sealed class SystemPromptFilterSpec : TenancySpecification<SystemPrompt>
{
    public SystemPromptFilterSpec(string? name, int isActive, int tenantId) : base(tenantId)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Query.Where(x => x.Name.Contains(name));
        }

        if (isActive != -1)
        {
            Query.Where(x => isActive == 0? x.IsActive == false: x.IsActive == true);
        }

        Query.OrderByDescending(x => x.IsActive)
             .ThenByDescending(x => x.LastModifiedAt);
    }
    public SystemPromptFilterSpec(string? name, int isActive, int tenantId, int pageIndex, int pageSize) : base(tenantId)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Query.Where(x => x.Name.Contains(name));
        }

        if (isActive != -1)
        {
            Query.Where(x => isActive == 0 ? x.IsActive == false : x.IsActive == true);
        }

        Query.OrderByDescending(m => m.LastModifiedAt).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
    }
}
