using ChatService.Events;
using ChatService.Features;
using ChatService.Hubs;
using Infrastructure.Authentication;
using Infrastructure.Tenancy;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Consumers;

/// <summary>
/// Consumes bot response messages from RabbitMQ.
/// Sets the tenant context before processing to ensure proper multi-tenancy isolation.
/// </summary>
public class BotResponseConsumer(
    ChatBusiness chatBusiness,
    IHubContext<ChatHub> hubContext,
    ICurrentTenantProvider currentTenantProvider,
    ILogger<BotResponseConsumer> logger) : IConsumer<BotResponseCreatedEvent>
{
    public async Task Consume(ConsumeContext<BotResponseCreatedEvent> context)
    {
        var botResponse = context.Message;

        try
        {
            logger.LogInformation(
                "RAW: Received bot response - ConversationId={ConversationId}, ModelUsed={ModelUsed}, MessageLength={MessageLength}",
                botResponse.ConversationId,
                botResponse.ModelUsed ?? "null",
                botResponse.Message?.Length ?? 0);

            var tokenInfo = TokenDecoder.DecodeJwtToken(botResponse.Token);
            var tenantId = TokenDecoder.GetTenantId(tokenInfo);
            currentTenantProvider.SetTenantId(tenantId);

            logger.LogInformation(
                "Received bot response for conversation {ConversationId}: {MessagePreview}",
                botResponse.ConversationId,
                botResponse.Message?.Length > 50 ? $"{botResponse.Message[..50]}..." : botResponse.Message ?? "");

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
