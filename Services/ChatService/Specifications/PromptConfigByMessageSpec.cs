using Ardalis.Specification;
using ChatService.Entities;
using Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Specifications;

/// <summary>
/// Specification to get prompt configurations where the user's message contains the config Key.
/// </summary>
public sealed class PromptConfigByMessageSpec : TenancySpecification<PromptConfig>
{
    public PromptConfigByMessageSpec(string message, int tenantId) : base(tenantId)
    {
        Query.Where(x => EF.Functions.Like(message, "%" + x.Key + "%"));
    }
}
