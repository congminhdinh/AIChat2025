using ChatService.Features;
using ChatService.Requests;
using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Endpoints
{
    public static class ChatEndpoint
    {
        public static void MapChatEndpoints(this WebApplication app)
        {
            app.MapChatApiGroups();
        }

        static void MapChatApiGroups(this IEndpointRouteBuilder app)
        {
            var group = app.MapWebApiGroup("chat");

            group.MapPost("/conversations", async (ChatBusiness chatBusiness, [FromBody] CreateConversationRequest input) =>
            {
                return await chatBusiness.CreateConversationAsync(input);
            });

            group.MapGet("/conversations/list", async (ChatBusiness chatBusiness) =>
            {
                return await chatBusiness.GetConversationsAsync();
            });

            group.MapGet("/conversations/{conversationId}", async (ChatBusiness chatBusiness, int conversationId) =>
            {
                return await chatBusiness.GetConversationByIdAsync(conversationId);
            });

            group.MapPost("/messages", async (ChatBusiness chatBusiness, [FromBody] SendMessageRequest input) =>
            {
                return await chatBusiness.SaveUserMessageAndPublishAsync(input);
            });
        }
    }
}
