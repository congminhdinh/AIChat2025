using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications;

/// <summary>
/// Specification for filtering and searching PromptConfig by keyword in Key or Value.
/// </summary>
public sealed class PromptConfigFilterSpec : TenancySpecification<PromptConfig>
{
    public PromptConfigFilterSpec(string? keyword, int tenantId) : base(tenantId)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            Query.Where(x => x.Key.Contains(keyword) || x.Value.Contains(keyword));
        }

        Query.OrderBy(x => x.Key);
    }

    public PromptConfigFilterSpec(string? key, int tenantId, int pageIndex, int pageSize) : base(tenantId)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            Query.Where(x => x.Key.Contains(key));
        }

        Query.OrderByDescending(m => m.LastModifiedAt).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
    }
}
