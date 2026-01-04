using ChatService.Data;
using ChatService.Dtos;
using ChatService.Entities;
using ChatService.Enums;
using ChatService.Events;
using ChatService.Requests;
using ChatService.Specifications;
using Infrastructure;
using Infrastructure.Authentication;
using Infrastructure.Tenancy;
using Infrastructure.Web;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChatService.Features;

/// <summary>
/// Business logic for chat operations.
/// Handles conversation and message management with RabbitMQ integration.
/// </summary>
public class ChatBusiness
{

    private readonly IRepository<ChatConversation> _conversationRepo;
    private readonly IRepository<ChatMessage> _messageRepo;
    private readonly IRepository<PromptConfig> _promptConfigRepo;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ILogger<ChatBusiness> _logger;
    private readonly SystemPromptBusiness _systemPromptBusiness;

    public ChatBusiness(IRepository<ChatConversation> conversationRepo, IRepository<ChatMessage> messageRepo, IRepository<PromptConfig> promptConfigRepo, IPublishEndpoint publishEndpoint, ICurrentUserProvider currentUserProvider, ILogger<ChatBusiness> logger, SystemPromptBusiness systemPromptBusiness)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _promptConfigRepo = promptConfigRepo;
        _publishEndpoint = publishEndpoint;
        _currentUserProvider = currentUserProvider;
        _logger = logger;
        _systemPromptBusiness = systemPromptBusiness;
    }

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    public async Task<BaseResponse<ConversationDto>> CreateConversationAsync(CreateConversationRequest request)
    {
        var userId = _currentUserProvider.UserId;
        var conversation = new ChatConversation(userId, request.Title)
        {
            TenantId = _currentUserProvider.TenantId
        };

        await _conversationRepo.AddAsync(conversation);

        return new BaseResponse<ConversationDto>(new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            LastMessageAt = conversation.LastMessageAt,
            MessageCount = 0,
            Messages = new()
        }, request.CorrelationId());
    }

    /// <summary>
    /// Gets all conversations for the current tenant.
    /// </summary>
    public async Task<BaseResponse<List<ConversationDto>>> GetConversationsAsync()
    {
        var request = new BaseRequest();
        var userId = _currentUserProvider.UserId;
        var spec = new GetConversationsByUserSpec(userId, _currentUserProvider.TenantId);
        var conversations = await _conversationRepo.ListAsync(spec);

        var conversationDtos = conversations.Select(c => new ConversationDto
        {
            Id = c.Id,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            LastMessageAt = c.LastMessageAt,
            MessageCount = c.Messages.Count,
            Messages = new()
        }).ToList();
        return new BaseResponse<List<ConversationDto>>(conversationDtos, request.CorrelationId());
    }

    /// <summary>
    /// Gets a conversation with all its messages.
    /// </summary>
    public async Task<BaseResponse<ConversationDto>> GetConversationByIdAsync(int conversationId)
    {
        var request= new GetConversationByIdRequest
        {
            ConversationId = conversationId
        };
        var spec = new GetConversationWithMessagesSpec(conversationId, _currentUserProvider.TenantId);
        var conversation = await _conversationRepo.FirstOrDefaultAsync(spec);

        if (conversation == null)
        {
            return new BaseResponse<ConversationDto>("Conversation not found", BaseResponseStatus.Error, request.CorrelationId());
        }

        return new BaseResponse<ConversationDto>(new ConversationDto
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
                }).ToList()
        }, request.CorrelationId());
    }

    /// <summary>
    /// Saves a user message to the database and publishes to RabbitMQ for AI processing.
    /// </summary>
    public async Task<MessageDto> SaveUserMessageAndPublishAsync(SendMessageRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserProvider.UserId;
        var message = new ChatMessage(request.ConversationId, request.Message, userId)
        {
            TenantId = _currentUserProvider.TenantId,
            Timestamp = DateTime.UtcNow,
            Type = ChatType.Request
        };

        await _messageRepo.AddAsync(message, ct);

        var conversation = await _conversationRepo.GetByIdAsync(request.ConversationId, ct);
        if (conversation != null)
        {
            conversation.LastMessageAt = message.Timestamp;
            await _conversationRepo.UpdateAsync(conversation, ct);
        }

        await _messageRepo.SaveChangesAsync(ct);
        var promptConfigSpec = new PromptConfigByMessageSpec(request.Message, _currentUserProvider.TenantId);
        var promptConfigs = await _promptConfigRepo.ListAsync(promptConfigSpec, ct);

        // Map to DTOs
        var systemInstructions = promptConfigs.Select(pc => new PromptConfigDto
        {
            Key = pc.Key,
            Value = pc.Value
        }).ToList();

        // Fetch active SystemPrompt for tenant (fallback to null if none exists)
        var activeSystemPrompt = await _systemPromptBusiness.GetActiveAsync(_currentUserProvider.TenantId);

        var userPromptEvent = new UserPromptReceivedEvent
        {
            ConversationId = request.ConversationId,
            Message = request.Message,
            Token = _currentUserProvider.Token?? string.Empty,
            Timestamp = message.Timestamp,
            SystemInstruction = systemInstructions,
            SystemPrompt = activeSystemPrompt
        };
        _logger.LogInformation($"Publishing UserPromptReceivedEvent: {JsonSerializer.Serialize(userPromptEvent)}");
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
    public async Task<MessageDto> SaveBotMessageAsync(BotResponseCreatedEvent botResponse, CancellationToken ct = default)
    {
        _logger.LogInformation($"Saving bot response: {JsonSerializer.Serialize(botResponse)}");
        var tokenInfo = TokenDecoder.DecodeJwtToken(botResponse.Token);
        var userId = TokenDecoder.GetUserId(tokenInfo); 
        var tenantId = TokenDecoder.GetTenantId(tokenInfo);
        var message = new ChatMessage(botResponse.ConversationId, botResponse.Message, userId)
        {
            TenantId = tenantId,
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
