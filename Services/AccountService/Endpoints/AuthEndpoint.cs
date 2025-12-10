using AccountService.Dtos;
using AccountService.Features;
using AccountService.Requests;
using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Endpoints
{
    public static class AuthEndpoint
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapWebApiGroups();
        }
        static void MapWebApiGroups(this IEndpointRouteBuilder app)
        {
            var group = app.MapWebApiGroup("accounts");
            group.MapGet("/ok", () => Results.Ok("Account service is running"));
            group.MapPost("/register", async (AuthBusiness authBusiness, [FromBody]RegisterRequest input, int tenantId) =>
            {
                return await authBusiness.Register(input, tenantId);
            }).AllowAnonymous();
            group.MapPost("/login", async (AuthBusiness authBusiness, LoginRequest input, int tenantId) =>
            {
                return await authBusiness.Login(input, tenantId);
            }).AllowAnonymous();
        }
    }
}
