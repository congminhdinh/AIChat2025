using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications;

/// <summary>
/// Specification to get the active SystemPrompt for a tenant.
/// </summary>
public sealed class SystemPromptActiveSpec : TenancySpecification<SystemPrompt>
{
    public SystemPromptActiveSpec(int tenantId) : base(tenantId)
    {
        Query.Where(x => x.IsActive);
    }
}
