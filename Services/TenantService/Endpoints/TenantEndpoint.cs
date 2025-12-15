using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;
using TenantService.Features;
using TenantService.Requests;

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
            var group = app.MapWebApiGroup("tenant");
            group.MapGet("/ok", () => Results.Ok("Tenant service is running")).AllowAnonymous();

            group.MapGet("/list", async (TenantBusiness tenantBusiness, GetTenantListRequest input) =>
            {
                return await tenantBusiness.GetTenantList(input);
            });

            group.MapPost("/create", async (TenantBusiness tenantBusiness, [FromBody] CreateTenantRequest input) =>
            {
                return await tenantBusiness.CreateTenant(input);
            });
        }
    }
}
