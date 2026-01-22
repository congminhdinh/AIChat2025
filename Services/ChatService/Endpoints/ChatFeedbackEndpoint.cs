using ChatService.Features;
using ChatService.Requests;
using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Endpoints
{
    public static class ChatFeedbackEndpoint
    {
        public static void MapChatFeedbackEndpoints(this WebApplication app)
        {
            app.MapChatFeedbackApiGroups();
        }

        static void MapChatFeedbackApiGroups(this IEndpointRouteBuilder app)
        {
            var group = app.MapWebApiGroup("chat");

            group.MapGet("/chat-feedback/", async (ChatFeedbackBusiness business, [AsParameters] GetChatFeedbackListRequest request) =>
            {
                return await business.GetListChatFeedback(request);
            });

            group.MapGet("/chat-feedback/{id}", async (ChatFeedbackBusiness business, int id) =>
            {
                var request = new GetChatFeedbackByIdRequest { Id = id };
                return await business.GetChatFeedbackDetail(request);
            });

            group.MapPost("/chat-feedback/", async (ChatFeedbackBusiness business, [FromBody] CreateChatFeedbackRequest request) =>
            {
                return await business.CreateChatFeedback(request);
            });

            group.MapPut("/chat-feedback/{id}", async (ChatFeedbackBusiness business, int id, [FromBody] UpdateChatFeedbackRequest request) =>
            {
                request.Id = id;
                return await business.UpdateChatFeedback(request);
            });

            group.MapPost("/chat-feedback/rate", async (ChatFeedbackBusiness business, [FromBody] RateChatFeedbackRequest request) =>
            {
                return await business.RateChatFeedBack(request);
            });
        }
    }
}
