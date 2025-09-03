using Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Web;


public interface ICurrentUserProvider
{
    int UserId { get; }
    string Username { get; }
    string? Scope { get; }
}

public class CurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private int _cachedUserId;
    private string _cachedScope ="";
    private string _cachedUsername = "";

    public int UserId => GetUserId();

    public string Username => GetUsername();

    public string Scope => GetScope();

    private int GetUserId()
    {
        if (_cachedUserId > 0)
        {
            return _cachedUserId;
        }

        var nameIdentifier = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(nameIdentifier, out int userId))
        {
            _cachedUserId = userId;
            return userId;
        }
        return 0;
    }
    private string GetScope()
    {
        if (!string.IsNullOrEmpty(_cachedScope))
        {
            return _cachedScope;
        }

        var scope = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(AuthorizationConstants.TOKEN_CLAIMS_TYPE_SCOPE);
        _cachedScope = scope ?? "";
        return _cachedScope;
    }

    private string GetUsername()
    {
        if (!string.IsNullOrEmpty(_cachedUsername))
        {
            return _cachedUsername;
        }

        var name = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
        _cachedUsername = name ?? "";
        return _cachedUsername;
    }
}
