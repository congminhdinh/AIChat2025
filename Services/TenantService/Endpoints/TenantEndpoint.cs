using Infrastructure.Web;

namespace TenantService.Endpoints
{
    public static class TenantEndpoint
    {
        public static void MapTenantEndpoints(this WebApplication app)
        {
            app.MapWebApiGroups();
        }
        static void MapWebApiGroups(this IEndpointRouteBuilder app)
        {
            var group = app.MapWebApiGroup("tenants/ok");
            group.MapGet("/", () => Results.Ok("Tenant service is running"));
        }
    }
}
