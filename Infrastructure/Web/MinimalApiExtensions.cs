using Infrastructure.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace Infrastructure.Web
{
    public class EndpointConstants
    {
        public const string ADMIN_API_BASE_ENDPOINT = "admin-api";
        public const string WEB_API_BASE_ENDPOINT = "web-api";
    }
    public static class MinimalApiExtensions
    {
        public static RouteGroupBuilder MapAdminApiGroup(this IEndpointRouteBuilder routes, string prefix)
        {
            return routes.MapGroup($"{EndpointConstants.ADMIN_API_BASE_ENDPOINT}/{prefix}")
                .WithTags(prefix)
                .RequireAuthorization(AuthorizationConstants.SCOPE_ADMIN);
        }
        public static RouteGroupBuilder MapWebApiGroup(this IEndpointRouteBuilder routes, string prefix)
        {
            return routes.MapGroup($"{EndpointConstants.WEB_API_BASE_ENDPOINT}/{prefix}")
                .WithTags(prefix)
                .RequireAuthorization(AuthorizationConstants.SCOPE_WEB);
        }
    }
}
