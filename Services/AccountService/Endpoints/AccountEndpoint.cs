using AccountService.Dtos;
using AccountService.Features;
using Infrastructure.Web;

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
            var group = app.MapWebApiGroup("accounts/ok");
            group.MapGet("/", () => Results.Ok("Account service is running"));
            group.MapPost("/register", async (AccountBusiness accountBusiness, RegisterDto input, int tenantId) =>
            {
                var result = await accountBusiness.Register(input, tenantId);
                return Results.Ok(result);
            }).WithName("Register").WithOpenApi();
        }
    }
}
