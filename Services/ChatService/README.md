# ChatService - .NET 9 AI Chat System

## Overview

ChatService is a .NET 9 microservice that acts as the **Producer** and **UI Gateway** for the AI Chat System. It handles user interactions via SignalR and REST API, coordinates with a Python AI processor through RabbitMQ, and manages conversation persistence.

## System Architecture

```
┌─────────────┐     SignalR/REST      ┌──────────────┐
│   Client    │ ◄───────────────────► │ ChatService  │
│ (Frontend)  │                        │   (.NET 9)   │
└─────────────┘                        └──────┬───────┘
                                              │
                    ┌─────────────────────────┴──────────────────────┐
                    │                                                 │
                    ▼                                                 ▼
            ┌──────────────┐                                  ┌──────────┐
            │  SQL Server  │                                  │ RabbitMQ │
            │  (Database)  │                                  └────┬─────┘
            └──────────────┘                                       │
                                                                   │
                                        ┌──────────────────────────┴────────┐
                                        │                                    │
                                        ▼                                    │
                            ┌──────────────────┐                           │
                            │  ChatProcessor   │                           │
                            │    (Python)      │ ──────────────────────────┘
                            │  + Ollama AI     │
                            └──────────────────┘
```

### Message Flow

1. **User sends message** → ChatService (SignalR or REST)
2. **ChatService** → Saves user message to DB → Publishes to `UserPromptReceived` queue
3. **Python ChatProcessor** → Consumes from `UserPromptReceived` → Processes with Ollama AI
4. **Python ChatProcessor** → Publishes response to `BotResponseCreated` queue
5. **ChatService** → Consumes from `BotResponseCreated` → Saves to DB → Broadcasts to client via SignalR

## Technology Stack

- **.NET 9** with C# 13
- **ASP.NET Core** - Minimal APIs
- **SignalR** - Real-time communication
- **Entity Framework Core 9** - ORM with SQL Server
- **MassTransit 8** - RabbitMQ messaging
- **Ardalis.Specification** - Repository pattern

## Project Structure

```
ChatService/
├── Data/                    # DbContext + Generic Repository
│   ├── ChatDbContext.cs
│   └── EfRepository.cs
├── Dtos/                    # Data Transfer Objects
│   ├── ConversationDto.cs
│   └── MessageDto.cs
├── Endpoints/               # Minimal API endpoint mappings
│   └── ChatEndpoint.cs
├── Entities/                # Domain entities
│   ├── ChatConversation.cs
│   └── ChatMessage.cs
├── Events/                  # MassTransit event contracts
│   ├── UserPromptReceivedEvent.cs
│   └── BotResponseCreatedEvent.cs
├── Features/                # Business logic classes
│   └── ChatBusiness.cs
├── Hubs/                    # SignalR Hubs
│   └── ChatHub.cs
├── Consumers/               # MassTransit Consumers
│   └── BotResponseConsumer.cs
├── Requests/                # Input models
│   ├── CreateConversationRequest.cs
│   └── SendMessageRequest.cs
├── Specifications/          # Ardalis Specification pattern
│   ├── GetConversationsByUserSpec.cs
│   └── GetConversationWithMessagesSpec.cs
├── Config/
│   └── appsettings.json
└── Program.cs               # DI registration & configuration
```

## Database Schema

### ChatConversation
- `Id` (PK, int)
- `Title` (string, max 500)
- `UserId` (int)
- `TenantId` (int)
- `LastMessageAt` (DateTime)
- `CreatedAt` (DateTime)
- `IsDeleted` (bool)

### ChatMessage
- `Id` (PK, int)
- `ConversationId` (FK → ChatConversation)
- `Message` (string, required)
- `UserId` (int) - 0 for bot messages
- `TenantId` (int)
- `Timestamp` (DateTime)
- `CreatedAt` (DateTime)
- `IsDeleted` (bool)

**Relationship**: One Conversation has Many Messages (1:n) with cascade delete.

## RabbitMQ Integration

### Outgoing Queue: `UserPromptReceived`
Published when a user sends a message.

**Payload:**
```json
{
  "conversationId": 123,
  "message": "What is the capital of France?",
  "userId": 456,
  "timestamp": "2025-12-16T10:30:00Z"
}
```

### Incoming Queue: `BotResponseCreated`
Consumed when Python service generates a response.

**Payload:**
```json
{
  "conversationId": 123,
  "message": "The capital of France is Paris.",
  "userId": 0,
  "timestamp": "2025-12-16T10:30:05Z",
  "modelUsed": "llama2"
}
```

## API Endpoints

### REST APIs

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/chat/conversations` | Create a new conversation |
| GET | `/api/chat/conversations/user/{userId}` | Get all conversations for a user |
| GET | `/api/chat/conversations/{conversationId}` | Get conversation with messages |
| POST | `/api/chat/messages` | Send a message (alternative to SignalR) |

### SignalR Hub: `/hubs/chat`

**Client → Server Methods:**
- `SendMessage(conversationId, message, userId)` - Send a user message
- `JoinConversation(conversationId)` - Join conversation group
- `LeaveConversation(conversationId)` - Leave conversation group

**Server → Client Methods:**
- `ReceiveMessage(messageDto)` - Receive user message broadcast
- `BotResponse(messageDto)` - Receive bot response

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "ChatDbContext": "Server=localhost;Database=AIChat2025;Integrated Security=True;"
  },
  "RabbitMQEndpoint": "localhost:5672",
  "RabbitMQUsername": "guest",
  "RabbitMQPassword": "guest"
}
```

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server
- RabbitMQ Server
- Python ChatProcessor service (with Ollama)

### Run the Service

```bash
cd Services/ChatService
dotnet restore
dotnet build
dotnet run
```

The service will be available at:
- API: `http://localhost:5000` (or configured port)
- SignalR Hub: `http://localhost:5000/hubs/chat`
- Swagger: `http://localhost:5000/swagger`

### Database Migration

```bash
# Create migration
dotnet ef migrations add InitialCreate --project ChatService.csproj

# Update database
dotnet ef database update --project ChatService.csproj
```

## Usage Examples

### REST API Example

**Create Conversation:**
```bash
curl -X POST http://localhost:5000/api/chat/conversations \
  -H "Content-Type: application/json" \
  -d '{
    "title": "My First Chat",
    "userId": 1
  }'
```

**Send Message:**
```bash
curl -X POST http://localhost:5000/api/chat/messages \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": 1,
    "message": "Hello AI!",
    "userId": 1
  }'
```

### SignalR Client Example (JavaScript)

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/hubs/chat")
    .build();

// Listen for messages
connection.on("ReceiveMessage", (message) => {
    console.log("User message:", message);
});

connection.on("BotResponse", (message) => {
    console.log("Bot response:", message);
});

// Connect and join conversation
await connection.start();
await connection.invoke("JoinConversation", 1);

// Send message
await connection.invoke("SendMessage", 1, "Hello AI!", 1);
```

## Architecture Patterns

### Repository Pattern with Specifications
Uses Ardalis.Specification for clean, reusable query logic:

```csharp
var spec = new GetConversationsByUserSpec(userId);
var conversations = await repository.ListAsync(spec);
```

### Primary Constructors (C# 12/13)
Reduces boilerplate in services and hubs:

```csharp
public class ChatBusiness(
    IRepository<ChatConversation> conversationRepo,
    IPublishEndpoint publishEndpoint)
{
    // Fields automatically created
}
```

### Minimal APIs
Clean, focused endpoint definitions:

```csharp
group.MapPost("/conversations", CreateConversation)
    .WithName("CreateConversation")
    .Produces<ConversationDto>(201);
```

## Key Features

✅ **Real-time Communication** - SignalR for instant message delivery
✅ **REST API Alternative** - Full REST support for non-WebSocket clients
✅ **Event-Driven Architecture** - RabbitMQ for async AI processing
✅ **Multi-tenancy Support** - Built-in tenant isolation
✅ **Soft Delete** - Entities support soft delete via IsDeleted flag
✅ **Audit Trail** - Automatic CreatedAt, CreatedBy tracking
✅ **CORS Enabled** - Configured for frontend integration
✅ **Swagger/OpenAPI** - Interactive API documentation

## Integration with Python Service

The ChatService must be running alongside the Python ChatProcessor service. Ensure:

1. **RabbitMQ** is running and accessible
2. **Queue names match exactly**:
   - Publish to: `UserPromptReceived`
   - Consume from: `BotResponseCreated`
3. **ConversationId is always included** in messages for proper routing

## Troubleshooting

### RabbitMQ Connection Issues
- Verify RabbitMQ is running: `rabbitmqctl status`
- Check endpoint in appsettings.json
- Ensure firewall allows port 5672

### SignalR Connection Failures
- Verify CORS settings in Program.cs
- Check client origin is allowed
- Ensure WebSocket support in hosting environment

### Database Issues
- Run migrations: `dotnet ef database update`
- Check connection string in appsettings.json
- Verify SQL Server is running

## Performance Considerations

- **SignalR Groups**: Users are grouped by conversation for efficient broadcasting
- **Async/Await**: All I/O operations are async
- **Specification Pattern**: Optimized queries with proper indexing
- **Connection Pooling**: EF Core uses connection pooling by default

## Security

- Enable authentication middleware (configured in Infrastructure)
- Validate all user inputs with DataAnnotations
- Use TenantId for multi-tenant data isolation
- Sanitize message content before storage
- Configure CORS for production environments

## License

[Your License Here]

## Contact

[Your Contact Information]
