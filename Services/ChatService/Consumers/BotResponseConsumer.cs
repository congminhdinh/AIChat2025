using ChatService.Events;
using ChatService.Features;
using ChatService.Hubs;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Consumers;

/// <summary>
/// MassTransit consumer that listens to BotResponseCreated queue.
/// Processes bot responses from Python ChatProcessor service.
/// Queue Name: BotResponseCreated
/// </summary>
public class BotResponseConsumer(
    ChatBusiness chatBusiness,
    IHubContext<ChatHub> hubContext,
    ILogger<BotResponseConsumer> logger) : IConsumer<BotResponseCreatedEvent>
{
    public async Task Consume(ConsumeContext<BotResponseCreatedEvent> context)
    {
        var botResponse = context.Message;

        try
        {
            logger.LogInformation(
                "Received bot response for conversation {ConversationId}: {MessagePreview}",
                botResponse.ConversationId,
                botResponse.Message.Length > 50 ? $"{botResponse.Message[..50]}..." : botResponse.Message);
            var messageDto = await chatBusiness.SaveBotMessageAsync(botResponse, context.CancellationToken);
            await ChatHub.BroadcastBotResponse(hubContext, botResponse.ConversationId, messageDto);

            logger.LogInformation(
                "Successfully processed and broadcast bot response for conversation {ConversationId}",
                botResponse.ConversationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error processing bot response for conversation {ConversationId}",
                botResponse.ConversationId);
            throw;
        }
    }
}
