using Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Web;


public interface ICurrentUserProvider
{
    int UserId { get; }
    int TenantId { get; }
    string Username { get; }
    string? Scope { get; }
    bool IsAdmin { get; }
}

public class CurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public int UserId => GetUserId();

    public int TenantId => GetTenantId();

    public string Username => GetUsername();

    public string Scope => GetScope();
    public bool IsAdmin => GetIsAdmin();

    private int GetUserId()
    {
        var nameIdentifier = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(nameIdentifier, out int userId))
        {
            return userId;
        }
        return 0;
    }
    private int GetTenantId()
    {
        var tenantClaim = _httpContextAccessor?.HttpContext?.User?
            .FindFirstValue(AuthorizationConstants.TOKEN_CLAIMS_TENANT);

        if (int.TryParse(tenantClaim, out int tenantId))
        {
            return tenantId;
        }
        return 0;
    }
    private string GetScope()
    {


        var scope = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(AuthorizationConstants.TOKEN_CLAIMS_TYPE_SCOPE);
        return scope ?? "";
    }

    private string GetUsername()
    {
        var name = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
        return name ?? "";
    }

    private bool GetIsAdmin()
    {
        var isAdmin = _httpContextAccessor?.HttpContext?.User?
            .FindFirstValue(AuthorizationConstants.POLICY_ADMIN)?? "False";
        return isAdmin.Equals("False") ? false : true;
    }
}
