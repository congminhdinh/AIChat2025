using AccountService.Dtos;
using AccountService.Features;
using AccountService.Requests;
using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Endpoints
{
    public static class AccountEndpoint
    {
        public static void MapAccountEndpoints(this WebApplication app)
        {
            app.MapWebApiGroups();
        }
        static void MapWebApiGroups(this IEndpointRouteBuilder app)
        {
            var group = app.MapWebApiGroup("accounts");
            group.MapGet("/ok", () => Results.Ok("Account service is running"));
            group.MapPost("/register", async (AccountBusiness accountBusiness, [FromBody]RegisterRequest input, int tenantId) =>
            {
                return await accountBusiness.Register(input, tenantId);
            }).AllowAnonymous();
            group.MapPost("/login", async (AccountBusiness accountBusiness, LoginRequest input, int tenantId) =>
            {
                return await accountBusiness.Login(input, tenantId);
            }).AllowAnonymous();
        }
    }
}
