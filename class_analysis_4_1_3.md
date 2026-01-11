# CLASS ANALYSIS FOR UML CLASS DIAGRAMS (4.1.3)

**Document**: Phan tich classes cho 2 UML Class Diagrams
**Version**: 1.0
**Date**: 2026-01-12
**Based on**: project_map.md, package_analysis_4.1.2.md

---

# DIAGRAM 1: MULTI-TENANT ISOLATION

## Infrastructure Package (Core)

### INTERFACE: ICurrentTenantProvider
File: ./Infrastructure/Tenancy/CurrentTenantProvider.cs:5
Type: Interface [MULTITENANT]

### CLASS: CurrentTenantProvider
File: ./Infrastructure/Tenancy/CurrentTenantProvider.cs:15
Type: Service class [MULTITENANT]

Relationships:
- CurrentTenantProvider --▷ ICurrentTenantProvider
  Evidence: Line 15
- CurrentTenantProvider -----> ICurrentUserProvider
  Evidence: Line 17 (private readonly dependency)

---

### INTERFACE: ICurrentUserProvider
File: ./Infrastructure/Web/CurrentUserProvider.cs:8
Type: Interface [MULTITENANT]

### CLASS: CurrentUserProvider
File: ./Infrastructure/Web/CurrentUserProvider.cs:18
Type: Service class [MULTITENANT]

Relationships:
- CurrentUserProvider --▷ ICurrentUserProvider
  Evidence: Line 18
- CurrentUserProvider -----> IHttpContextAccessor
  Evidence: Line 18 (primary constructor)

---

### CLASS: BaseEntity
File: ./Infrastructure/Entities/BaseEntity.cs:3
Type: Abstract Entity

Properties: Id

### CLASS: AuditableEntity
File: ./Infrastructure/Entities/BaseEntity.cs:7
Type: Abstract Entity

Relationships:
- AuditableEntity ──▷ BaseEntity
  Evidence: Line 7

Properties: CreatedAt, LastModifiedAt, CreatedBy, LastModifiedBy, IsDeleted

### CLASS: TenancyEntity
File: ./Infrastructure/Entities/BaseEntity.cs:16
Type: Abstract Entity [MULTITENANT]

Relationships:
- TenancyEntity ──▷ AuditableEntity
  Evidence: Line 16

Properties: TenantId

---

### CLASS: TenancySpecification<T>
File: ./Infrastructure/Specifications/TenancySpecification.cs:6
Type: Specification class [MULTITENANT]

Relationships:
- TenancySpecification<T> ──▷ Specification<T>
  Evidence: Line 6 (Ardalis.Specification)

---

### CLASS: UpdateTenancyInterceptor
File: ./Infrastructure/Database/UpdateTenancyInterceptor.cs:9
Type: EF Core Interceptor [MULTITENANT]

Relationships:
- UpdateTenancyInterceptor ──▷ SaveChangesInterceptor
  Evidence: Line 9
- UpdateTenancyInterceptor -----> IServiceScopeFactory
  Evidence: Line 9 (primary constructor)
- UpdateTenancyInterceptor -----> ICurrentTenantProvider
  Evidence: Line 24 (resolved from DI)

---

### CLASS: BaseDbContext
File: ./Infrastructure/Database/BaseDbContext.cs:9
Type: DbContext base [MULTITENANT]

Relationships:
- BaseDbContext ──▷ DbContext
  Evidence: Line 9

---

### STATIC CLASS: TenantHashConstant
File: ./Infrastructure/Tenancy/TenantHashConstant.cs:3
Type: Constants [MULTITENANT]

---

### INTERFACE: ITokenClaimsService
File: ./Infrastructure/Authentication/TokenClaimsService.cs:9
Type: Interface

### CLASS: TokenClaimsService
File: ./Infrastructure/Authentication/TokenClaimsService.cs:13
Type: Service class [MULTITENANT]

Relationships:
- TokenClaimsService --▷ ITokenClaimsService
  Evidence: Line 13

---

### CLASS: BaseMessage
File: ./Infrastructure/BaseRequest.cs:3
Type: Abstract class

### CLASS: BaseRequest
File: ./Infrastructure/BaseRequest.cs:11
Type: DTO base

Relationships:
- BaseRequest ──▷ BaseMessage
  Evidence: Line 11

### CLASS: BaseResponse
File: ./Infrastructure/BaseResponse.cs:5
Type: Response wrapper

Relationships:
- BaseResponse ──▷ BaseMessage
  Evidence: Line 5

### CLASS: BaseResponse<T>
File: ./Infrastructure/BaseResponse.cs:25
Type: Generic Response wrapper

Relationships:
- BaseResponse<T> ──▷ BaseResponse
  Evidence: Line 25

---

## AccountService Package

### CLASS: Account
File: ./Services/AccountService/Entities/Account.cs:6
Type: Entity [MULTITENANT]

Relationships:
- Account ──▷ TenancyEntity
  Evidence: Line 6

---

### CLASS: AccountDbContext
File: ./Services/AccountService/Data/AccountDbContext.cs:8
Type: DbContext [MULTITENANT]

Relationships:
- AccountDbContext ──▷ BaseDbContext
  Evidence: Line 8

---

### STATIC CLASS: AccountEndpoint
File: ./Services/AccountService/Endpoints/AccountEndpoint.cs:8
Type: Minimal API Endpoint

Relationships:
- AccountEndpoint -----> AccountBusiness
  Evidence: Line 20-59

---

## TenantService Package

### CLASS: Tenant
File: ./Services/TenantService/Entities/Tenant.cs:5
Type: Entity

Relationships:
- Tenant ──▷ AuditableEntity
  Evidence: Line 5

Note: Tenant entity does NOT inherit TenancyEntity (it IS the tenant)

---

### CLASS: TenantDbContext
File: ./Services/TenantService/Data/TenantDbContext.cs:7
Type: DbContext

Relationships:
- TenantDbContext ──▷ BaseDbContext
  Evidence: Line 7

---

### STATIC CLASS: TenantEndpoint
File: ./Services/TenantService/Endpoints/TenantEndpoint.cs:8
Type: Minimal API Endpoint

Relationships:
- TenantEndpoint -----> TenantBusiness
  Evidence: Line 19-42

---

## CROSS-PACKAGE DEPENDENCIES

1. AccountDbContext → BaseDbContext (Infrastructure)
   Type: Inheritance [MULTITENANT CRITICAL]
   Evidence: ./Services/AccountService/Data/AccountDbContext.cs:8
   Purpose: Inherits soft-delete query filter from base

2. Account → TenancyEntity (Infrastructure)
   Type: Inheritance [MULTITENANT CRITICAL]
   Evidence: ./Services/AccountService/Entities/Account.cs:6
   Purpose: Adds TenantId property for row-level isolation

3. CurrentTenantProvider → ICurrentUserProvider (Infrastructure)
   Type: Dependency [MULTITENANT]
   Evidence: ./Infrastructure/Tenancy/CurrentTenantProvider.cs:17
   Purpose: Extract TenantId from JWT claims via HttpContext

4. UpdateTenancyInterceptor → ICurrentTenantProvider (Infrastructure)
   Type: Dependency [MULTITENANT]
   Evidence: ./Infrastructure/Database/UpdateTenancyInterceptor.cs:24
   Purpose: Auto-set TenantId on SaveChanges

5. TenancySpecification → EF.Property<int> (Infrastructure)
   Type: Query Filter [MULTITENANT]
   Evidence: ./Infrastructure/Specifications/TenancySpecification.cs:13
   Purpose: Filter queries by TenantId at runtime

---

## DIAGRAM 1 LAYOUT

```
┌─────────────────────────────────────────────────────────────────┐
│              INFRASTRUCTURE (Shared Library)                     │
│                                                                  │
│  ┌─────────────────────┐     ┌────────────────────────┐        │
│  │ ICurrentTenantProvider│◄────│ CurrentTenantProvider  │        │
│  └─────────────────────┘     └────────────────────────┘        │
│            ▲                           │                        │
│            │                           │ uses                   │
│            │                           ▼                        │
│  ┌─────────────────────┐     ┌────────────────────────┐        │
│  │ ICurrentUserProvider │◄────│ CurrentUserProvider    │        │
│  └─────────────────────┘     └────────────────────────┘        │
│                                        │                        │
│                                        │ uses                   │
│                                        ▼                        │
│                              ┌────────────────────────┐        │
│                              │ IHttpContextAccessor   │        │
│                              └────────────────────────┘        │
│                                                                  │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                    ENTITY HIERARCHY                        │ │
│  │  ┌────────────┐                                            │ │
│  │  │ BaseEntity │                                            │ │
│  │  └─────┬──────┘                                            │ │
│  │        │ extends                                           │ │
│  │        ▼                                                    │ │
│  │  ┌─────────────────┐                                       │ │
│  │  │ AuditableEntity │                                       │ │
│  │  └───────┬─────────┘                                       │ │
│  │          │ extends                                          │ │
│  │          ▼                                                  │ │
│  │  ┌─────────────────┐                                       │ │
│  │  │ TenancyEntity   │ [MULTITENANT]                         │ │
│  │  │ + TenantId      │                                       │ │
│  │  └─────────────────┘                                       │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌─────────────────────┐     ┌────────────────────────┐        │
│  │ BaseDbContext       │     │ TenancySpecification<T>│        │
│  └─────────────────────┘     └────────────────────────┘        │
│            ▲                                                    │
│            │                                                    │
│  ┌─────────────────────┐                                       │
│  │UpdateTenancyInterceptor│                                    │
│  └─────────────────────┘                                       │
└─────────────────────────────────────────────────────────────────┘
              │                               │
              │ inherits                      │ inherits
              ▼                               ▼
┌─────────────────────────────┐   ┌─────────────────────────────┐
│     AccountService          │   │     TenantService           │
│                             │   │                             │
│  ┌───────────────────┐     │   │  ┌───────────────────┐     │
│  │ AccountDbContext  │     │   │  │ TenantDbContext   │     │
│  │ ──▷ BaseDbContext │     │   │  │ ──▷ BaseDbContext │     │
│  └───────────────────┘     │   │  └───────────────────┘     │
│            │               │   │            │               │
│            │ has           │   │            │ has           │
│            ▼               │   │            ▼               │
│  ┌───────────────────┐     │   │  ┌───────────────────┐     │
│  │ Account           │     │   │  │ Tenant            │     │
│  │ ──▷ TenancyEntity │     │   │  │ ──▷ AuditableEntity│     │
│  │ [MULTITENANT]     │     │   │  │                   │     │
│  └───────────────────┘     │   │  └───────────────────┘     │
└─────────────────────────────┘   └─────────────────────────────┘
```

---

## STATISTICS - DIAGRAM 1

- Total Classes: 18
- [MULTITENANT] Classes: 11
- Interfaces: 4
- Cross-package Dependencies: 5

---

# DIAGRAM 2: RAG PIPELINE

## ChatService Package (.NET)

### CLASS: ChatHub
File: ./Services/ChatService/Hubs/ChatHub.cs:11
Type: SignalR Hub [RAG]

Relationships:
- ChatHub ──▷ Hub
  Evidence: Line 11
- ChatHub -----> ChatBusiness
  Evidence: Line 11 (primary constructor)
- ChatHub -----> ILogger<ChatHub>
  Evidence: Line 11 (primary constructor)

---

### CLASS: BotResponseConsumer
File: ./Services/ChatService/Consumers/BotResponseConsumer.cs:15
Type: MassTransit Consumer [RAG] [MULTITENANT]

Relationships:
- BotResponseConsumer --▷ IConsumer<BotResponseCreatedEvent>
  Evidence: Line 19
- BotResponseConsumer -----> ChatBusiness
  Evidence: Line 16
- BotResponseConsumer -----> IHubContext<ChatHub>
  Evidence: Line 17
- BotResponseConsumer -----> ICurrentTenantProvider
  Evidence: Line 18 (tenant impersonation for background process)

---

### CLASS: ChatBusiness
File: ./Services/ChatService/Features/ChatBusiness.cs:25
Type: Business Service [RAG] [MULTITENANT]

Relationships:
- ChatBusiness ──▷ BaseHttpClient
  Evidence: Line 25
- ChatBusiness -----> IRepository<ChatConversation>
  Evidence: Line 28
- ChatBusiness -----> IRepository<ChatMessage>
  Evidence: Line 29
- ChatBusiness -----> IRepository<PromptConfig>
  Evidence: Line 30
- ChatBusiness -----> IPublishEndpoint
  Evidence: Line 32 (MassTransit - publishes to RabbitMQ)
- ChatBusiness -----> ICurrentUserProvider
  Evidence: Line 33

---

### CLASS: ChatConversation
File: ./Services/ChatService/Entities/ChatConversation.cs:5
Type: Entity [RAG] [MULTITENANT]

Relationships:
- ChatConversation ──▷ TenancyEntity
  Evidence: Line 5
- ChatConversation ◇----> ChatMessage
  Evidence: Line 22 (one-to-many)

---

### CLASS: ChatMessage
File: ./Services/ChatService/Entities/ChatMessage.cs:5
Type: Entity [RAG] [MULTITENANT]

Relationships:
- ChatMessage ──▷ TenancyEntity
  Evidence: Line 5
- ChatMessage -----> ChatConversation
  Evidence: Line 26 (navigation property)

---

### CLASS: PromptConfig
File: ./Services/ChatService/Entities/PromptConfig.cs:5
Type: Entity [RAG] [MULTITENANT]

Relationships:
- PromptConfig ──▷ TenancyEntity
  Evidence: Line 5

---

### CLASS: ChatDbContext
File: ./Services/ChatService/Data/ChatDbContext.cs:7
Type: DbContext [RAG] [MULTITENANT]

Relationships:
- ChatDbContext ──▷ BaseDbContext
  Evidence: Line 7

---

### RECORD: UserPromptReceivedEvent
File: ./Services/ChatService/Events/UserPromptReceivedEvent.cs:5
Type: Message Event [RAG]

Purpose: Published to RabbitMQ when user sends message

---

### RECORD: BotResponseCreatedEvent
File: ./Services/ChatService/Events/BotResponseCreatedEvent.cs:3
Type: Message Event [RAG]

Purpose: Consumed from RabbitMQ when ChatProcessor responds

---

## ChatProcessor Package (Python)

### CLASS: RabbitMQService
File: ./Services/ChatProcessor/src/consumer.py:10
Type: Service class [RAG]

Relationships:
- RabbitMQService -----> aio_pika.AbstractConnection
  Evidence: Line 21
- RabbitMQService -----> aio_pika.AbstractChannel
  Evidence: Line 22

---

### CLASS: QdrantService
File: ./Services/ChatProcessor/app/services/qdrant_service.py:9
Type: Service class [RAG] [MULTITENANT]

Relationships:
- QdrantService -----> QdrantClient
  Evidence: Line 15
- QdrantService -----> httpx (EmbeddingService call)
  Evidence: Line 136

---

### CLASS: OllamaService
File: ./Services/ChatProcessor/app/services/ollama_service.py:7
Type: Service class [RAG]

Relationships:
- OllamaService -----> httpx.AsyncClient
  Evidence: Line 28

---

### CLASS: UserPromptReceivedMessage
File: ./Services/ChatProcessor/app/models/messages.py:5
Type: Pydantic Model [RAG]

Relationships:
- UserPromptReceivedMessage ──▷ BaseModel
  Evidence: Line 5

---

### CLASS: BotResponseCreatedMessage
File: ./Services/ChatProcessor/app/models/messages.py:12
Type: Pydantic Model [RAG]

Relationships:
- BotResponseCreatedMessage ──▷ BaseModel
  Evidence: Line 12

---

## EmbeddingService Package (Python)

### CLASS: EmbeddingService
File: ./Services/EmbeddingService/src/business.py:9
Type: Service class [RAG] [MULTITENANT]

Relationships:
- EmbeddingService -----> AutoTokenizer
  Evidence: Line 14 (Hugging Face Transformers)
- EmbeddingService -----> AutoModel
  Evidence: Line 15 (Hugging Face Transformers)
- EmbeddingService -----> QdrantClient
  Evidence: Line 23

---

### CLASS: EmbeddingRequest
File: ./Services/EmbeddingService/src/schemas.py:4
Type: Pydantic Model [RAG]

### CLASS: VectorizeRequest
File: ./Services/EmbeddingService/src/schemas.py:7
Type: Pydantic Model [RAG]

### CLASS: BatchVectorizeRequest
File: ./Services/EmbeddingService/src/schemas.py:12
Type: Pydantic Model [RAG]

### CLASS: EmbeddingResponse
File: ./Services/EmbeddingService/src/schemas.py:22
Type: Pydantic Model [RAG]

### CLASS: VectorizeResponse
File: ./Services/EmbeddingService/src/schemas.py:26
Type: Pydantic Model [RAG]

### CLASS: SearchRequest
File: ./Services/EmbeddingService/src/schemas.py:34
Type: Pydantic Model [RAG]

---

## CROSS-PACKAGE DEPENDENCIES (RAG)

1. ChatBusiness → IPublishEndpoint (MassTransit)
   Type: Async Publish [RAG]
   Evidence: ./Services/ChatService/Features/ChatBusiness.cs:194
   Message: UserPromptReceivedEvent → RabbitMQ

2. BotResponseConsumer → ICurrentTenantProvider (Infrastructure)
   Type: Dependency [MULTITENANT]
   Evidence: ./Services/ChatService/Consumers/BotResponseConsumer.cs:35
   Purpose: Set tenant context for background processing

3. BotResponseConsumer → IHubContext<ChatHub>
   Type: SignalR Broadcast [RAG]
   Evidence: ./Services/ChatService/Consumers/BotResponseConsumer.cs:43
   Purpose: Push bot response to connected clients

4. RabbitMQService (Python) → RabbitMQ
   Type: Async Consume/Publish [RAG]
   Evidence: ./Services/ChatProcessor/src/consumer.py:51,92
   Purpose: Receive user prompts, send bot responses

5. QdrantService (Python) → Qdrant Vector DB
   Type: Vector Search [RAG]
   Evidence: ./Services/ChatProcessor/app/services/qdrant_service.py:28
   Purpose: Semantic search with tenant filter

6. QdrantService (Python) → EmbeddingService
   Type: HTTP [RAG]
   Evidence: ./Services/ChatProcessor/app/services/qdrant_service.py:136
   Purpose: Get embeddings for query

7. OllamaService (Python) → Ollama LLM
   Type: HTTP [RAG]
   Evidence: ./Services/ChatProcessor/app/services/ollama_service.py:29
   Purpose: Generate response using retrieved context

8. EmbeddingService → QdrantClient
   Type: Vector Storage [RAG]
   Evidence: ./Services/EmbeddingService/src/business.py:23
   Purpose: Store document embeddings

9. EmbeddingService → Transformers (HuggingFace)
   Type: Model Inference [RAG]
   Evidence: ./Services/EmbeddingService/src/business.py:14-15
   Purpose: Generate embeddings from text

---

## DIAGRAM 2 LAYOUT

```
┌─────────────────────────────────────────────────────────────────┐
│                ChatService (.NET)                                │
│                                                                  │
│  ┌──────────────┐     ┌──────────────┐     ┌────────────────┐  │
│  │  ChatHub     │◄────│ ChatBusiness │────▶│ IPublishEndpoint│  │
│  │  (SignalR)   │     │              │     │  (MassTransit)  │  │
│  │  [RAG]       │     │  [RAG]       │     └────────────────┘  │
│  └──────────────┘     └──────────────┘            │            │
│         ▲                    │                     │ publish    │
│         │                    │                     ▼            │
│         │             ┌──────────────┐     ┌────────────────┐  │
│         │             │ ChatDbContext│     │   RabbitMQ     │  │
│         │             └──────────────┘     │ (UserPrompt    │  │
│         │                    │             │  ReceivedEvent)│  │
│         │                    │             └────────────────┘  │
│         │                    ▼                     │            │
│  ┌──────────────────────────────────────┐         │            │
│  │ Entities:                             │         │            │
│  │ ChatConversation ──▷ TenancyEntity   │         │            │
│  │ ChatMessage ──▷ TenancyEntity        │         │            │
│  │ PromptConfig ──▷ TenancyEntity       │         │            │
│  └──────────────────────────────────────┘         │            │
│                                                    │            │
│  ┌────────────────────┐          ┌────────────────┼──────────┐│
│  │BotResponseConsumer │◄─────────│   RabbitMQ     │          ││
│  │ [RAG][MULTITENANT] │          │ (BotResponse   │          ││
│  │                    │          │  CreatedEvent) │          ││
│  └────────────────────┘          └────────────────┼──────────┘│
│         │                                          │            │
│         │ broadcast via SignalR                   │            │
│         └──────────────────────────────────────────┘            │
└─────────────────────────────────────────────────────────────────┘
                              │ consume
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│               ChatProcessor (Python)                             │
│                                                                  │
│  ┌──────────────────┐                                           │
│  │ RabbitMQService  │──────────────────────────────────────┐   │
│  │                  │                                       │   │
│  └──────────────────┘                                       │   │
│         │                                                    │   │
│         │ on message                                        │   │
│         ▼                                                    │   │
│  ┌──────────────────┐     ┌──────────────────┐             │   │
│  │ QdrantService    │────▶│ EmbeddingService │             │   │
│  │ [RAG][MULTITENANT]│     │ (HTTP call)      │             │   │
│  └──────────────────┘     └──────────────────┘             │   │
│         │                                                    │   │
│         │ vector search                                     │   │
│         ▼                                                    │   │
│  ┌──────────────────┐                                       │   │
│  │ Qdrant Vector DB │                                       │   │
│  └──────────────────┘                                       │   │
│         │                                                    │   │
│         │ retrieved context                                 │   │
│         ▼                                                    │   │
│  ┌──────────────────┐                                       │   │
│  │ OllamaService    │────▶ Ollama LLM                       │   │
│  │ [RAG]            │                                       │   │
│  └──────────────────┘                                       │   │
│         │                                                    │   │
│         │ generated response                                │   │
│         └──────────────────────────────────────────────────►│   │
│                                                   publish    │   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│             EmbeddingService (Python)                            │
│                                                                  │
│  ┌──────────────────┐     ┌──────────────────┐                 │
│  │ EmbeddingService │────▶│ Transformers     │                 │
│  │ [RAG][MULTITENANT]│     │ (AutoModel)      │                 │
│  └──────────────────┘     └──────────────────┘                 │
│         │                                                        │
│         │ store vectors                                         │
│         ▼                                                        │
│  ┌──────────────────┐                                           │
│  │ QdrantClient     │────▶ Qdrant Vector DB                     │
│  └──────────────────┘                                           │
└─────────────────────────────────────────────────────────────────┘
```

---

## STATISTICS - DIAGRAM 2

- Total Classes: 20
- .NET Classes: 11
- Python Classes/Modules: 9
- [RAG] Classes: 20 (100%)
- [MULTITENANT] Classes: 7
- Async Dependencies: 9

---

## COMBINED STATISTICS

### Package Counts
```
┌─────────────────────────────┬───────┬─────────────┬───────┐
│ PACKAGE                     │ Total │ MULTITENANT │ RAG   │
├─────────────────────────────┼───────┼─────────────┼───────┤
│ Infrastructure              │   14  │     10      │   0   │
│ AccountService              │    3  │      3      │   0   │
│ TenantService               │    3  │      1      │   0   │
│ ChatService                 │   11  │      7      │  11   │
│ ChatProcessor (Python)      │    5  │      1      │   5   │
│ EmbeddingService (Python)   │    7  │      1      │   7   │
├─────────────────────────────┼───────┼─────────────┼───────┤
│ TOTAL                       │   43  │     23      │  23   │
└─────────────────────────────┴───────┴─────────────┴───────┘
```

### UML Relationship Types Used
```
──▷  : Inheritance/Implements (solid with closed arrowhead)
----> : Dependency/Association (dashed arrow)
◇----> : Aggregation (diamond with arrow)
```

---

*Generated by Claude Code - Phase 3 Class Analysis*
