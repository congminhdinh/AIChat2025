using ChatService.Features;
using ChatService.Requests;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Hubs;

/// <summary>
/// SignalR Hub for real-time chat communication.
/// Handles user messages and broadcasts bot responses.
/// </summary>
public class ChatHub(ChatBusiness chatBusiness, ILogger<ChatHub> logger) : Hub
{
    private const string ReceiveMessageMethod = "ReceiveMessage";
    private const string BotResponseMethod = "BotResponse";

    /// <summary>
    /// Called when a user sends a message.
    /// Saves the message, publishes to RabbitMQ, and notifies all clients in the conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="message">The message content</param>
    /// <param name="userId">The user ID sending the message</param>
    public async Task SendMessage(int conversationId, string message, int userId)
    {
        try
        {
            logger.LogInformation(
                "User {UserId} sending message to conversation {ConversationId}",
                userId, conversationId);

            // Save user message and publish to RabbitMQ
            var request = new SendMessageRequest
            {
                ConversationId = conversationId,
                Message = message,
            };

            var messageDto = await chatBusiness.SaveUserMessageAndPublishAsync(request, Context.ConnectionAborted);

            // Notify all clients in this conversation group
            await Clients
                .Group($"conversation-{conversationId}")
                .SendAsync(ReceiveMessageMethod, messageDto, Context.ConnectionAborted);

            logger.LogInformation(
                "Message {MessageId} sent successfully for conversation {ConversationId}",
                messageDto.Id, conversationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error sending message to conversation {ConversationId} from user {UserId}",
                conversationId, userId);
            throw;
        }
    }

    /// <summary>
    /// Joins a conversation group for real-time updates.
    /// </summary>
    public async Task JoinConversation(int conversationId)
    {
        var groupName = $"conversation-{conversationId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        logger.LogInformation(
            "Connection {ConnectionId} joined conversation {ConversationId}",
            Context.ConnectionId, conversationId);
    }

    /// <summary>
    /// Leaves a conversation group.
    /// </summary>
    public async Task LeaveConversation(int conversationId)
    {
        var groupName = $"conversation-{conversationId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        logger.LogInformation(
            "Connection {ConnectionId} left conversation {ConversationId}",
            Context.ConnectionId, conversationId);
    }

    /// <summary>
    /// Called when a client connects.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            logger.LogWarning(exception,
                "Client {ConnectionId} disconnected with error",
                Context.ConnectionId);
        }
        else
        {
            logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Helper method to broadcast bot response to a conversation.
    /// Called by BotResponseConsumer.
    /// </summary>
    public static async Task BroadcastBotResponse(IHubContext<ChatHub> hubContext, int conversationId, object messageDto)
    {
        await hubContext.Clients
            .Group($"conversation-{conversationId}")
            .SendAsync(BotResponseMethod, messageDto);
    }
}
