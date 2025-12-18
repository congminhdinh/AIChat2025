using ChatService.Events;
using ChatService.Features;
using ChatService.Hubs;
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
                "RAW: Received bot response - ConversationId={ConversationId}, UserId={UserId}, TenantId={TenantId}, ModelUsed={ModelUsed}, MessageLength={MessageLength}",
                botResponse.ConversationId,
                botResponse.UserId,
                botResponse.TenantId,
                botResponse.ModelUsed ?? "null",
                botResponse.Message?.Length ?? 0);

            // IMPORTANT: Set the TenantId in the scoped ICurrentTenantProvider
            // This allows the UpdateTenancyInterceptor and other tenant-aware services
            // to function correctly in this background context (where HttpContext is not available)
            currentTenantProvider.SetTenantId(botResponse.TenantId);

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
