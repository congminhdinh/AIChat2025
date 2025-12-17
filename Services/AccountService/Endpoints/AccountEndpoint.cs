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
            app.MapAccountApiGroups();
        }

        static void MapAccountApiGroups(this IEndpointRouteBuilder app)
        {

            var group = app.MapWebApiGroup("account");

            group.MapGet("/{id}", async (AccountBusiness accountBusiness, int id) =>
            {
                return await accountBusiness.GetAccountById(new GetAccountByIdRequest { AccountId = id });
            });
            group.MapGet("/list", async (AccountBusiness accountBusiness, [AsParameters] GetAccountListRequest input) =>
            {
                return await accountBusiness.GetAccountList(input);
            });

            group.MapPost("/", async (AccountBusiness accountBusiness, [FromBody] CreateAccountRequest input) =>
            {
                return await accountBusiness.CreateAccount(input);
            });

            group.MapPost("/admin-account", async (AccountBusiness accountBusiness, [FromBody] CreateAdminAccountRequest input) =>
            {
                return await accountBusiness.CreateAdminAccount(input);
            });
            group.MapPut("/change-password", async (AccountBusiness accountBusiness, [FromBody] ChangePasswordRequest input) =>
            {
                return await accountBusiness.ChangePassword(input);
            });
            group.MapPut("/", async (AccountBusiness accountBusiness, [FromBody] UpdateAccountRequest input) =>
            {
                return await accountBusiness.UpdateAccount(input);
            });

            ///chỉ cho super admin
            group.MapPost("/tenancy-deactivate", async (AccountBusiness accountBusiness, int tenantId) =>
            {
                return await accountBusiness.DisableTenancy(tenantId);
            });
        }
    }
}
