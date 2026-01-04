using ChatService.Features;
using ChatService.Requests;
using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Endpoints
{
    public static class SystemPromptEndpoint
    {
        public static void MapSystemPromptEndpoints(this WebApplication app)
        {
            app.MapSystemPromptApiGroups();
        }

        static void MapSystemPromptApiGroups(this IEndpointRouteBuilder app)
        {
            var group = app.MapWebApiGroup("chat");

            group.MapGet("/system-prompt/", async (SystemPromptBusiness business, [AsParameters] GetListSystemPromptRequest request) =>
            {
                return await business.GetListAsync(request);
            });

            group.MapGet("/system-prompt/{id}", async (SystemPromptBusiness business, int id) =>
            {
                return await business.GetByIdAsync(new GetSystemPromptByIdRequest { Id = id});
            });

            group.MapPost("/system-prompt/", async (SystemPromptBusiness business, [FromBody] CreateSystemPromptRequest request) =>
            {
                return await business.CreateAsync(request);
            });

            group.MapPut("/system-prompt/{id}", async (SystemPromptBusiness business, [FromBody] UpdateSystemPromptRequest request) =>
            {
                return await business.UpdateAsync(request);
            });

            group.MapDelete("/system-prompt/{id}", async (SystemPromptBusiness business, int id) =>
            {
                var deleteRequest = new DeleteSystemPromptRequest { Id = id };
                return await business.DeleteAsync(deleteRequest);
            });
        }
    }
}
