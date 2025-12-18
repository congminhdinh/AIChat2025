using ChatService.Features;
using ChatService.Requests;
using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Endpoints
{
    public static class PromptConfigEndpoint
    {
        public static void MapPromptConfigEndpoints(this WebApplication app)
        {
            app.MapPromptConfigApiGroups();
        }

        static void MapPromptConfigApiGroups(this IEndpointRouteBuilder app)
        {
            var group = app.MapWebApiGroup("chat");

            group.MapGet("/prompt-config/", async (PromptConfigBusiness business, [AsParameters] GetListPromptConfiRequest request) =>
            {
                return await business.GetListAsync(request);
            });

            group.MapPost("/prompt-config/", async (PromptConfigBusiness business, [FromBody] CreatePromptConfigRequest request) =>
            {
                return await business.CreateAsync(request);
            });

            group.MapPut("/prompt-config/{id}", async (PromptConfigBusiness business, int id, [FromBody] CreatePromptConfigRequest request) =>
            {
                var updateRequest = new UpdatePromptConfigRequest
                {
                    Id = id,
                    Key = request.Key,
                    Value = request.Value
                };
                return await business.UpdateAsync(updateRequest);
            });

            group.MapDelete("/prompt-config/{id}", async (PromptConfigBusiness business, int id) =>
            {
                var deleteRequest = new DeletePromptConfigRequest { Id = id };
                return await business.DeleteAsync(deleteRequest);
            });
        }
    }
}
