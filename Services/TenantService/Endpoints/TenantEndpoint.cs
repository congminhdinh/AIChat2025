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

            group.MapGet("/list", async ([FromServices]TenantBusiness tenantBusiness, [AsParameters]GetTenantListRequest input) =>
            {
                return await tenantBusiness.GetTenantList(input);
            });

            group.MapGet("/{id}", async ([FromServices] TenantBusiness tenantBusiness, int id) =>
            {
                return await tenantBusiness.GetTenantById(new GetTenantByIdRequest { Id = id});
            });

            group.MapPost("/create", async (TenantBusiness tenantBusiness, CreateTenantRequest input) =>
            {
                return await tenantBusiness.CreateTenant(input);
            });

            group.MapPost("/update", async (TenantBusiness tenantBusiness, UpdateTenantRequest input) =>
            {
                return await tenantBusiness.UpdateTenant(input);
            });

            group.MapPost("/deactivate", async (TenantBusiness tenantBusiness, DeactivateTenantRequest input) =>
            {
                return await tenantBusiness.DeactivateTenant(input);
            });
        }
    }
}
