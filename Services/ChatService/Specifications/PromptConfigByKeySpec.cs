using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;

namespace ChatService.Specifications;

/// <summary>
/// Specification to check if a PromptConfig with a specific Key already exists.
/// Used for duplicate validation during Create/Update operations.
/// </summary>
public sealed class PromptConfigByKeySpec : TenancySpecification<PromptConfig>
{
    public PromptConfigByKeySpec(string key, int tenantId, int? excludeId = null) : base(tenantId)
    {
        Query.Where(x => x.Key == key);

        if (excludeId.HasValue)
        {
            Query.Where(x => x.Id != excludeId.Value);
        }
    }
}
