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
        }
    }
}
