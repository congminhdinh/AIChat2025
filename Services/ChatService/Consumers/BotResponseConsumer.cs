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
            // ═══ CONSOLE LOG: RECEIVED FROM RABBITMQ ═══
            Console.WriteLine("\n" + new string('═', 80));
            Console.WriteLine("📥 BOT RESPONSE RECEIVED FROM RABBITMQ");
            Console.WriteLine(new string('═', 80));
            Console.WriteLine($"  Queue:           BotResponseCreated");
            Console.WriteLine($"  Conversation ID: {botResponse.ConversationId}");
            Console.WriteLine($"  Model Used:      {botResponse.ModelUsed ?? "N/A"}");
            Console.WriteLine($"  Response Length: {botResponse.Message.Length} characters");
            Console.WriteLine($"  Preview:         {botResponse.Message.Substring(0, Math.Min(100, botResponse.Message.Length))}{(botResponse.Message.Length > 100 ? "..." : "")}");
            Console.WriteLine(new string('═', 80) + "\n");

            logger.LogInformation(
                "Received bot response for conversation {ConversationId}: {MessagePreview}",
                botResponse.ConversationId,
                botResponse.Message.Length > 50 ? $"{botResponse.Message[..50]}..." : botResponse.Message);

            // Save bot message to database
            var messageDto = await chatBusiness.SaveBotMessageAsync(botResponse, context.CancellationToken);

            // Broadcast to all connected SignalR clients in the conversation
            await ChatHub.BroadcastBotResponse(hubContext, botResponse.ConversationId, messageDto);

            // ═══ CONSOLE LOG: BROADCAST COMPLETE ═══
            Console.WriteLine("\n" + new string('─', 80));
            Console.WriteLine("✅ BOT RESPONSE SAVED & BROADCAST TO SIGNALR");
            Console.WriteLine(new string('─', 80));
            Console.WriteLine($"  Conversation ID: {botResponse.ConversationId}");
            Console.WriteLine($"  Message ID:      {messageDto.Id}");
            Console.WriteLine($"  Broadcast to:    conversation-{botResponse.ConversationId} group");
            Console.WriteLine(new string('─', 80) + "\n");

            logger.LogInformation(
                "Successfully processed and broadcast bot response for conversation {ConversationId}",
                botResponse.ConversationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error processing bot response for conversation {ConversationId}",
                botResponse.ConversationId);

            // Let MassTransit handle retry logic
            throw;
        }
    }
}
