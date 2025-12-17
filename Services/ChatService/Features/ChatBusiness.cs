using ChatService.Data;
using ChatService.Dtos;
using ChatService.Entities;
using ChatService.Enums;
using ChatService.Events;
using ChatService.Requests;
using ChatService.Specifications;
using Infrastructure.Tenancy;
using Infrastructure.Web;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Features;

/// <summary>
/// Business logic for chat operations.
/// Handles conversation and message management with RabbitMQ integration.
/// </summary>
public class ChatBusiness
{

    private readonly IRepository<ChatConversation> _conversationRepo;
    private readonly IRepository<ChatMessage> _messageRepo;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICurrentUserProvider _currentUserProvider;

    public ChatBusiness(IRepository<ChatConversation> conversationRepo, IRepository<ChatMessage> messageRepo, IPublishEndpoint publishEndpoint, ICurrentUserProvider currentUserProvider)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _publishEndpoint = publishEndpoint;
        _currentUserProvider = currentUserProvider;
    }

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    public async Task<ConversationDto> CreateConversationAsync(CreateConversationRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserProvider.UserId;
        var conversation = new ChatConversation(userId, request.Title)
        {
            TenantId = _currentUserProvider.TenantId
        };

        await _conversationRepo.AddAsync(conversation, ct);

        return new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            LastMessageAt = conversation.LastMessageAt,
            MessageCount = 0,
            Messages = new()
        };
    }

    /// <summary>
    /// Gets all conversations for the current tenant.
    /// </summary>
    public async Task<List<ConversationDto>> GetConversationsAsync(CancellationToken ct = default)
    {
        var userId = _currentUserProvider.UserId;
        var spec = new GetConversationsByUserSpec(userId);
        var conversations = await _conversationRepo.ListAsync(spec, ct);

        return conversations.Select(c => new ConversationDto
        {
            Id = c.Id,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            LastMessageAt = c.LastMessageAt,
            MessageCount = c.Messages.Count,
            Messages = new()
        }).ToList();
    }

    /// <summary>
    /// Gets a conversation with all its messages.
    /// </summary>
    public async Task<ConversationDto?> GetConversationByIdAsync(int conversationId, CancellationToken ct = default)
    {
        var spec = new GetConversationWithMessagesSpec(conversationId);
        var conversation = await _conversationRepo.FirstOrDefaultAsync(spec, ct);

        if (conversation == null)
            return null;

        return new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            LastMessageAt = conversation.LastMessageAt,
            MessageCount = conversation.Messages.Count,
            Messages = conversation.Messages
                .OrderBy(m => m.Timestamp)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    Content = m.Message,
                    Timestamp = m.Timestamp,
                    UserId = m.UserId,
                    Type = m.Type
                })
                .ToList()
        };
    }

    /// <summary>
    /// Saves a user message to the database and publishes to RabbitMQ for AI processing.
    /// </summary>
    public async Task<MessageDto> SaveUserMessageAndPublishAsync(SendMessageRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserProvider.UserId;
        // Save user message to database
        var message = new ChatMessage(request.ConversationId, request.Message, userId)
        {
            TenantId = _currentUserProvider.TenantId,
            Timestamp = DateTime.UtcNow,
            Type = ChatType.Request
        };

        await _messageRepo.AddAsync(message, ct);

        // Update conversation's LastMessageAt
        var conversation = await _conversationRepo.GetByIdAsync(request.ConversationId, ct);
        if (conversation != null)
        {
            conversation.LastMessageAt = message.Timestamp;
            await _conversationRepo.UpdateAsync(conversation, ct);
        }

        await _messageRepo.SaveChangesAsync(ct);

        // Publish to RabbitMQ for Python service to process
        var userPromptEvent = new UserPromptReceivedEvent
        {
            ConversationId = request.ConversationId,
            Message = request.Message,
            UserId = userId,
            Timestamp = message.Timestamp,
            TenantId = _currentUserProvider.TenantId
        };

        await _publishEndpoint.Publish(userPromptEvent, ct);

        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Content = message.Message,
            Timestamp = message.Timestamp,
            UserId = message.UserId,
            Type = message.Type
        };
    }

    /// <summary>
    /// Saves a bot response message to the database.
    /// Called by BotResponseConsumer when receiving from RabbitMQ.
    /// </summary>
    public async Task<MessageDto> SaveBotMessageAsync(BotResponseCreatedEvent botResponse, CancellationToken ct = default)
    {
        // Save bot message to database
        var message = new ChatMessage(botResponse.ConversationId, botResponse.Message, botResponse.UserId)
        {
            TenantId = _currentUserProvider.TenantId,
            Timestamp = botResponse.Timestamp,
            Type = ChatType.Response
        };

        await _messageRepo.AddAsync(message, ct);

        // Update conversation's LastMessageAt
        var conversation = await _conversationRepo.GetByIdAsync(botResponse.ConversationId, ct);
        if (conversation != null)
        {
            conversation.LastMessageAt = message.Timestamp;
            await _conversationRepo.UpdateAsync(conversation, ct);
        }

        await _messageRepo.SaveChangesAsync(ct);

        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Content = message.Message,
            Timestamp = message.Timestamp,
            UserId = message.UserId,
            Type = message.Type
        };
    }
}
