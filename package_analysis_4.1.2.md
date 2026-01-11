# AICHAT2025 - PACKAGE DIAGRAM ANALYSIS (4.1.2)

**Document**: Phan tich packages cho bieu do UML Package Diagram
**Version**: 1.0
**Date**: 2026-01-11
**Based on**: project_map.md (Phase 1 Discovery)

---

## Overview

Phan tich packages cho **1 diagram duy nhat** the hien toan bo he thong AIChat2025 voi **6 tiers**, **nhan manh** dac thu **Multi-tenant** va **RAG pipeline**.

### Architecture Summary
```
┌─────────────────────────────────────────────────────────────────┐
│                  AICHAT2025 SYSTEM PACKAGES                      │
│         Multi-tenant RAG Architecture (Package View)             │
├─────────────────────────────────────────────────────────────────┤
│  TIER 1: PRESENTATION         │ 2 packages (WebApp, AdminCMS)   │
│  TIER 2: API GATEWAY          │ 1 package  (ApiGateway)         │
│  TIER 3: APPLICATION SERVICES │ 6 packages (.NET Microservices) │
│  TIER 4: AI PROCESSING        │ 2 packages (Python Services)    │
│  TIER 5: MESSAGE QUEUE        │ 1 package  (RabbitMQ)           │
│  TIER 6: DATA & STORAGE       │ 4 packages (SQL, Qdrant, etc.)  │
└─────────────────────────────────────────────────────────────────┘
```

---

## TIER 1: PRESENTATION LAYER

### Overview
Tang giao dien nguoi dung, gom cac ung dung web ASP.NET MVC chay tren trinh duyet, giao tiep voi backend qua API Gateway thong qua HTTPS.

---

### Package 1.1: WebApp (Tenant Portal)

**Full Name**: `AIChat.Frontend.WebApp`
**Location**: `./WebApp/WebApp.csproj`
**Type**: ASP.NET MVC Application
**Framework**: .NET 9

**Purpose**:
Ung dung web danh cho nguoi dung cuoi cua tung tenant, cung cap giao dien hoi dap AI (RAG Chat), quan ly tai lieu, va xem lich su hoi thoai.

**Multi-tenant**: [MULTITENANT]
- Truy cap qua subdomain hoac URL rieng theo tenant
- Tu dong nhan dien tenant tu JWT token (TenantId claim)
- UI hien thi thong tin tenant-specific

**RAG Features**: [RAG]
- Chat interface voi real-time streaming response (SignalR)
- Document viewer
- Conversation history management
- Prompt configuration per tenant

**Target Users**: Nhan vien cua tenant (end users)

**Key Controllers**:
```
Controllers/
├── HomeController.cs      - Dashboard, landing page
├── AuthController.cs      - Login, logout, register
├── ChatController.cs      - RAG chat interface [RAG]
├── DocumentController.cs  - Document management [RAG]
├── AccountController.cs   - User profile management
├── PromptConfigController.cs - RAG prompt settings [RAG]
├── SystemPromptController.cs - System prompt config [RAG]
└── StatController.cs      - Statistics dashboard
```

**Dependencies**:
- → API Gateway [HTTPS] - Calls /web-api/chat/*, /web-api/document/*
- → SignalR Hub [WebSocket] - Real-time chat streaming via /hubs/chat

---

### Package 1.2: AdminCMS (Admin Portal)

**Full Name**: `AIChat.Frontend.AdminCMS`
**Location**: `./AdminCMS/AdminCMS.csproj`
**Type**: ASP.NET MVC Application
**Framework**: .NET 9

**Purpose**:
Ung dung quan tri danh cho super admin, cho phep quan ly tat ca tenants, nguoi dung, tai lieu tri thuc, va cau hinh he thong.

**Multi-tenant**: [MULTITENANT]
- Truy cap qua admin URL (khong subdomain)
- Co quyen xem va quan ly TAT CA tenants
- Tenant selector trong UI de chon tenant can quan ly

**RAG Features**: [RAG]
- Upload/manage documents cho tung tenant
- Quan ly cau hinh RAG (prompt templates, settings)
- Trigger document reprocessing (re-chunking, re-embedding)

**Target Users**: Super Admin, System Administrator

**Key Controllers**:
```
Controllers/
├── HomeController.cs      - Admin dashboard
├── AuthController.cs      - Admin authentication
├── TenantController.cs    - Tenant CRUD [MULTITENANT]
├── AccountController.cs   - User management per tenant [MULTITENANT]
├── DocumentController.cs  - Document management [RAG]
└── ChatController.cs      - Chat monitoring [RAG]
```

**Dependencies**:
- → API Gateway [HTTPS] - Calls /web-api/tenant/*, /web-api/document/*, /web-api/account/*

---

### Tier 1 Statistics
```
Total Frontend Projects: 2
├── [MULTITENANT] Projects: 2 (100%)
└── [RAG] Projects: 2 (100%)
```

---

## TIER 2: API GATEWAY

### Overview
Cong API duy nhat cua he thong, dong vai tro trung gian giua Frontend va Backend Services, thuc hien authentication, routing va load balancing.

---

### Package 2.1: ApiGateway (YARP Reverse Proxy)

**Full Name**: `AIChat.Gateway.ApiGateway`
**Location**: `./ApiGateway/ApiGateway.csproj`
**Type**: YARP Reverse Proxy
**Framework**: .NET 9
**Docker Port**: 5000:8080

**Purpose**:
Nhan tat ca HTTP requests tu Frontend, thuc hien xac thuc JWT, va dinh tuyen requests den microservice phu hop.

**Multi-tenant**: [MULTITENANT]
- **Tenant Context Propagation**: Truyen TenantId tu JWT claim den downstream services
- **Tenant-aware Routing**: Route requests den service tuong ung

**RAG**: N/A (chi routing, khong xu ly RAG logic)

**Key Responsibilities**:
1. **Authentication**: Validate JWT token (Bearer)
2. **Authorization**: Check user permissions
3. **Request Routing**: Route to appropriate microservice
4. **SignalR Proxy**: Forward WebSocket connections to ChatService

**Key Packages**:
```
ApiGateway/
├── Config/
│   └── appsettings.json   - YARP routes configuration
├── Controllers/           - Health check endpoints
└── Program.cs             - YARP setup, middleware pipeline
```

**Routing Rules** (from appsettings.json):
```
Routes:
├── /web-api/account/*    → AccountService (http://accountservice:8080)
├── /web-api/tenant/*     → TenantService  (http://tenantservice:8080)
├── /web-api/document/*   → DocumentService (http://documentservice:8080) [RAG]
├── /web-api/storage/*    → StorageService (http://storageservice:8080)
├── /web-api/chat/*       → ChatService (http://chatservice:8080) [RAG]
└── /hubs/chat/*          → ChatService (http://chatservice:8080) [RAG] [SignalR]
```

**Dependencies**:
- → All .NET Microservices [HTTP Routing]
- → ChatService [WebSocket] - SignalR hub proxy

---

### Tier 2 Statistics
```
Total Gateway Projects: 1
├── [MULTITENANT]: Yes (context propagation)
└── [RAG]: N/A (routing only)
```

---

## TIER 3: APPLICATION SERVICES (.NET Microservices)

### Overview
Tang nghiep vu ung dung, gom 5 microservices doc lap + 1 shared library. Tat ca services dung chung database AIChat2025 tren SQL Server (shared database pattern).

---

### Package 3.0: Infrastructure (Shared Library)

**Full Name**: `AIChat.Infrastructure`
**Location**: `./Infrastructure/Infrastructure.csproj`
**Type**: .NET Class Library
**Framework**: .NET 9

**Purpose**:
Thu vien dung chung chua cac abstractions, utilities, va cross-cutting concerns duoc su dung boi tat ca microservices.

**Multi-tenant**: [MULTITENANT]
- `TenancyEntity` - Base class voi TenantId property
- `CurrentTenantProvider` - Inject tenant context tu JWT claims hoac manual set
- `TenancySpecification<T>` - Auto-filter queries by TenantId
- `UpdateTenancyInterceptor` - Auto-set TenantId on save

**RAG**: N/A

**Key Packages**:
```
Infrastructure/
├── Entities/
│   └── BaseEntity.cs          - BaseEntity, AuditableEntity, TenancyEntity [MULTITENANT]
├── Tenancy/
│   ├── CurrentTenantProvider.cs    - ICurrentTenantProvider [MULTITENANT]
│   └── TenantHashConstant.cs       - Tenant constants [MULTITENANT]
├── Specifications/
│   └── TenancySpecification.cs     - Query filter by TenantId [MULTITENANT]
├── Database/
│   ├── BaseDbContext.cs            - EF Core base context
│   ├── UpdateTenancyInterceptor.cs - Auto-set TenantId [MULTITENANT]
│   └── UpdateAuditableInterceptor.cs - Auto-set audit fields
├── Authentication/
│   ├── AuthenticationExtensions.cs - JWT setup
│   ├── TokenClaimsService.cs       - Token generation
│   └── TokenDecoder.cs             - Token parsing
├── Web/
│   ├── CurrentUserProvider.cs      - User context from HttpContext
│   └── BaseHttpClient.cs           - HTTP client base class
├── Paging/
│   └── PagedExtensions.cs          - Pagination utilities
├── Utils/                          - Helper classes
├── BaseRequest.cs                  - Base DTO for requests
└── BaseResponse.cs                 - Standard API response wrapper
```

**Entity Hierarchy** [MULTITENANT]:
```csharp
BaseEntity           → Id
    └── AuditableEntity  → CreatedAt, LastModifiedAt, CreatedBy, LastModifiedBy, IsDeleted
        └── TenancyEntity    → TenantId [MULTITENANT]
```

**Dependencies**:
- ← All .NET Services reference this library

---

### Package 3.1: AccountService

**Full Name**: `AIChat.Services.AccountService`
**Location**: `./Services/AccountService/AccountService.csproj`
**Type**: .NET 9 WebAPI
**Framework**: .NET 9
**Docker Port**: 5051:8080

**Purpose**:
Quan ly xac thuc (authentication), nguoi dung (users), va quyen han (permissions). Dam bao moi user operation deu tuan thu tenant boundaries.

**Multi-tenant**: [MULTITENANT]
- **Account entity** extends TenancyEntity (co TenantId)
- **Row-level security**: Moi query tu dong filter theo TenantId
- **Cross-tenant prevention**: Khong the truy cap users cua tenant khac

**RAG**: N/A

**Database**: SQL Server `AIChat2025` (AccountDbContext)

**Key Packages**:
```
AccountService/
├── Entities/
│   └── Account.cs         - User entity [MULTITENANT]
├── Endpoints/
│   └── AuthEndpoint.cs    - Login, register, token refresh
├── Requests/
│   ├── LoginRequest.cs
│   ├── RegisterRequest.cs
│   ├── CreateAccountRequest.cs
│   └── UpdateAccountRequest.cs
├── Dtos/
│   ├── CurrentUserDto.cs
│   └── TokenDto.cs
├── Data/
│   └── EfRepository.cs    - Repository pattern
├── Specifications/
│   └── AccountSpecification.cs
└── Migrations/            - EF Core migrations
```

**API Endpoints**:
```
POST /api/auth/login        - User authentication
POST /api/auth/register     - User registration
GET  /api/account           - List accounts (tenant-filtered) [MULTITENANT]
GET  /api/account/{id}      - Get account by ID
POST /api/account           - Create account
PUT  /api/account/{id}      - Update account
POST /api/account/change-password - Change password
```

**Dependencies**:
- → Infrastructure [Reference]

---

### Package 3.2: TenantService

**Full Name**: `AIChat.Services.TenantService`
**Location**: `./Services/TenantService/TenantService.csproj`
**Type**: .NET 9 WebAPI
**Framework**: .NET 9
**Docker Port**: 5062:8080

**Purpose**:
Quan ly thong tin tenant, cau hinh he thong theo tenant, va cung cap tenant data cho cac services khac.

**Multi-tenant**: [MULTITENANT]
- **Central authority** cho tenant information
- Manages **tenant-specific configurations**
- Provides **Permission** management per tenant

**RAG**: N/A (config storage only)

**Database**: SQL Server `AIChat2025` (TenantDbContext)

**Key Packages**:
```
TenantService/
├── Entities/
│   └── Permission.cs      - Permission entity
├── Requests/
│   ├── GetTenantByIdRequest.cs
│   └── GetTenantListRequest.cs
├── Data/
│   ├── TenantDbContext.cs
│   └── EfRepository.cs
├── Specifications/
│   └── TenantSpecificaton.cs
└── Migrations/
```

**API Endpoints**:
```
GET  /api/tenant           - List all tenants [MULTITENANT]
GET  /api/tenant/{id}      - Get tenant by ID
POST /api/tenant           - Create tenant
PUT  /api/tenant/{id}      - Update tenant
```

**Dependencies**:
- → Infrastructure [Reference]
- ← Other services depend on this for tenant validation

---

### Package 3.3: ChatService

**Full Name**: `AIChat.Services.ChatService`
**Location**: `./Services/ChatService/ChatService.csproj`
**Type**: .NET 9 WebAPI
**Framework**: .NET 9
**Docker Port**: 5218:8080

**Purpose**:
Xu ly nghiep vu hoi dap, luu lich su chat, quan ly prompt config, va ket noi voi RAG pipeline qua RabbitMQ + SignalR.

**Multi-tenant**: [MULTITENANT]
- **ChatConversation entity** extends TenancyEntity (co TenantId)
- **PromptConfig entity** - tenant-specific prompt templates
- Each tenant has **isolated chat history**

**RAG**: [RAG]
- **RAG Query Orchestration**: Receives user query, saves message, publishes to RabbitMQ
- **Async RAG Processing**: ChatProcessor consumes message and generates response
- **Response Streaming**: Receives response via MassTransit Consumer, broadcasts via SignalR
- **Prompt Configuration**: Manages RAG prompts per tenant

**Database**: SQL Server `AIChat2025` (ChatDbContext)

**Key Packages**:
```
ChatService/
├── Entities/
│   ├── ChatConversation.cs    - Chat entity [MULTITENANT]
│   └── PromptConfig.cs        - Prompt templates [RAG]
├── Hubs/
│   └── ChatHub.cs             - SignalR hub for real-time [RAG]
├── Consumers/
│   └── BotResponseConsumer.cs - MassTransit consumer [RAG]
├── Requests/
│   ├── CreateConversationRequest.cs
│   ├── SendMessageRequest.cs
│   └── PromptConfigRequests.cs
├── Dtos/
│   ├── ConversationDto.cs
│   └── PromptConfigDto.cs
├── Features/
│   └── ChatBusiness.cs        - Business logic
├── Data/
│   └── EfRepository.cs
├── Specifications/
│   ├── GetConversationsByUserSpec.cs
│   ├── GetConversationWithMessagesSpec.cs
│   └── PromptConfigByMessageSpec.cs
└── Migrations/
```

**SignalR Hub Methods** [RAG]:
```csharp
ChatHub:
├── SendMessage(conversationId, message, userId)  - User sends message
├── JoinConversation(conversationId)              - Join chat room
├── LeaveConversation(conversationId)             - Leave chat room
└── BroadcastBotResponse(conversationId, messageDto) - Bot response
```

**Message Flow** [RAG]:
```
User → SignalR Hub → Save Message → Publish to RabbitMQ
                                         ↓
                                   ChatProcessor (Python)
                                         ↓
                                   RabbitMQ Response
                                         ↓
BotResponseConsumer → SignalR Broadcast → User
```

**API Endpoints**:
```
GET  /api/chat/conversations          - List conversations [MULTITENANT]
GET  /api/chat/conversations/{id}     - Get conversation with messages
POST /api/chat/conversations          - Create conversation
POST /api/chat/messages               - Send message (triggers RAG) [RAG]

GET  /api/prompt-config               - Get prompt config [RAG]
POST /api/prompt-config               - Create/update prompt config [RAG]
```

**Dependencies**:
- → Infrastructure [Reference]
- → RabbitMQ [Async Publish] - Publishes chat messages [RAG]
- ← RabbitMQ [Consume] - Receives bot responses [RAG]

---

### Package 3.4: DocumentService

**Full Name**: `AIChat.Services.DocumentService`
**Location**: `./Services/DocumentService/DocumentService.csproj`
**Type**: .NET 9 WebAPI
**Framework**: .NET 9
**Docker Port**: 5165:8080

**Purpose**:
Quan ly tai lieu tri thuc (CRUD), phat hien cau truc phan cap van ban phap ly (Chuong-Muc-Dieu-Khoan), va kich hoat xu ly embedding.

**Multi-tenant**: [MULTITENANT]
- **Document entity** extends TenancyEntity (co TenantId)
- Each tenant has **isolated document library**
- **Metadata isolation** per tenant

**RAG**: [RAG]
- **Document Upload**: Receives file uploads, stores metadata
- **Hierarchy Detection**: Detects Vietnamese legal document structure
  - Regex patterns: `Chuong [IVXLCDM]+`, `Muc \d+`, `Dieu \d+`
- **Trigger Embedding**: Calls EmbeddingService to generate vectors
- **Background Jobs**: Uses Hangfire for async document processing
- **Qdrant Integration**: Syncs document vectors with Qdrant

**Database**: SQL Server `AIChat2025` (DocumentDbContext)

**Key Packages**:
```
DocumentService/
├── Entities/
│   └── Document entity     - Document metadata [MULTITENANT]
├── Dtos/
│   ├── DocumentChunkDto.cs - Chunk data [RAG]
│   └── StringValueDto.cs
├── Data/
│   ├── DocumentDbContext.cs
│   └── EfRepository.cs
├── Config/
│   └── appsettings.json    - Regex patterns for hierarchy [RAG]
└── Migrations/
```

**Hierarchy Detection Regex** [RAG]:
```json
{
  "RegexHeading1": "^\\s*Chuong\\s+[IVXLCDM]+\\b",
  "RegexHeading2": "^\\s*Muc\\s+\\d+\\b",
  "RegexHeading3": "^\\s*Dieu\\s+\\d+\\b"
}
```

**API Endpoints**:
```
GET  /api/document              - List documents [MULTITENANT]
GET  /api/document/{id}         - Get document by ID
POST /api/document/upload       - Upload document [RAG]
PUT  /api/document/{id}         - Update document metadata
DELETE /api/document/{id}       - Delete document (+ vectors)
POST /api/document/{id}/reprocess - Reprocess document [RAG]
```

**Dependencies**:
- → Infrastructure [Reference]
- → StorageService [HTTP] - Upload/download files
- → EmbeddingService [HTTP] - Generate embeddings [RAG]
- → Qdrant [Client] - Vector operations [RAG]

---

### Package 3.5: StorageService

**Full Name**: `AIChat.Services.StorageService`
**Location**: `./Services/StorageService/StorageService.csproj`
**Type**: .NET 9 WebAPI
**Framework**: .NET 9
**Docker Port**: 5113:8080

**Purpose**:
Quan ly upload/download files, tich hop voi MinIO object storage.

**Multi-tenant**: [MULTITENANT]
- Files stored with tenant prefix in bucket
- Access control via presigned URLs
- Bucket: `ai-chat-2025`

**RAG**: N/A (chi luu tru files, khong xu ly RAG logic)

**Key Packages**:
```
StorageService/
├── Dtos/
│   └── StringValueDto.cs
├── Config/
│   └── appsettings.json    - MinIO configuration
└── [References Infrastructure]
```

**MinIO Configuration**:
```json
{
  "MinioEndpoint": "minio:9000",
  "MinioBucket": "ai-chat-2025",
  "DocumentFilePath": "/app/storages"
}
```

**API Endpoints**:
```
POST /api/storage/upload      - Upload file
GET  /api/storage/{id}        - Download file (presigned URL)
DELETE /api/storage/{id}      - Delete file
```

**Dependencies**:
- → Infrastructure [Reference]
- → MinIO [Object Storage] - File storage [MULTITENANT]

---

### Tier 3 Statistics
```
Total .NET Microservices: 5 + 1 shared library = 6 packages
├── [MULTITENANT] Services: 6 (100%)
├── [RAG] Services: 2 (ChatService, DocumentService)
└── Database: 1 (shared AIChat2025)
```

---

## TIER 4: AI PROCESSING (Python Services)

### Overview
Tang xu ly AI, gom 2 Python services chuyen trach xu ly RAG pipeline va embedding. Giao tiep voi .NET services qua RabbitMQ (async) va HTTP (sync).

---

### Package 4.1: ChatProcessor

**Full Name**: `AIChat.AI.ChatProcessor`
**Location**: `./Services/ChatProcessor/`
**Type**: Python FastAPI Service
**Framework**: Python 3.11 + FastAPI 0.115
**Docker Port**: 8001:8001

**Purpose**:
Xu ly RAG pipeline hoan chinh tu query expansion → hybrid search → ranking → LLM generation → response cleanup.

**Multi-tenant**: [MULTITENANT]
- Receives **TenantId** trong message tu RabbitMQ
- Queries **tenant-filtered data** from Qdrant
- Uses **tenant-specific prompt config**

**RAG**: [RAG]
- **RAG Pipeline Steps**:
  1. **Receive Query**: Consume message from RabbitMQ (chat_query_queue)
  2. **Hybrid Search**: Vector search (Qdrant) + keyword matching
  3. **Context Building**: Structure retrieved chunks
  4. **LLM Generation**: Call Ollama API with context + query
  5. **Response Cleanup**: Format and clean response
  6. **Publish Response**: Send back via RabbitMQ

**Key Packages** (Python modules):
```
ChatProcessor/
├── app/
│   ├── main.py                    - FastAPI entry point
│   ├── models/
│   │   ├── __init__.py
│   │   └── messages.py            - Pydantic models
│   └── services/
│       ├── __init__.py
│       ├── ollama_service.py      - Ollama LLM client [RAG]
│       ├── qdrant_service.py      - Qdrant search [RAG]
│       └── rabbitmq_service.py    - RabbitMQ client
├── src/
│   ├── consumer.py                - Message consumer
│   ├── evaluation_service.py      - RAG evaluation (ragas) [RAG]
│   └── logger.py                  - Logging
├── tests/
│   └── test_hybrid_search.py      - Search tests [RAG]
├── main.py                        - Alternative entry
├── requirements.txt
└── Dockerfile
```

**Key Dependencies**:
```
fastapi==0.115.0
uvicorn==0.32.0
aio-pika==9.4.3          - RabbitMQ async client
httpx==0.27.0            - HTTP client for Ollama
pydantic==2.9.2
qdrant-client==1.11.3    - Vector search [RAG]
PyJWT==2.8.0             - Token handling
ragas                    - RAG evaluation metrics [RAG]
datasets                 - Evaluation datasets
```

**External Connections**:
- → RabbitMQ [Consume] - Consumes chat messages
- → Qdrant [Vector Search] - Semantic search [RAG + MULTITENANT]
- → Ollama [HTTP] - LLM generation (http://ollama:11434) [RAG]
- → RabbitMQ [Publish] - Sends bot responses

---

### Package 4.2: EmbeddingService

**Full Name**: `AIChat.AI.EmbeddingService`
**Location**: `./Services/EmbeddingService/`
**Type**: Python FastAPI Service
**Framework**: Python 3.11 + FastAPI
**Docker Port**: 8000:8000

**Purpose**:
Xu ly sinh vectors embedding tu text, luu vao Qdrant cho semantic search.

**Multi-tenant**: [MULTITENANT]
- Receives **TenantId** trong request
- Stores vectors voi **tenant metadata** in Qdrant

**RAG**: [RAG]
- **Embedding Pipeline**:
  1. **Receive Text**: HTTP request with text chunks
  2. **Generate Vectors**: Use ONNX-optimized sentence-transformers
  3. **Store in Qdrant**: Save vectors with metadata (tenant_id, doc_id, etc.)

**Key Packages** (Python modules):
```
EmbeddingService/
├── src/
│   ├── business.py        - Embedding logic [RAG]
│   ├── config.py          - Configuration
│   ├── logger.py          - Logging
│   ├── router.py          - FastAPI routes
│   └── schemas.py         - Pydantic models
├── main.py                - FastAPI entry
├── requirements.txt
└── Dockerfile
```

**Key Dependencies**:
```
fastapi
uvicorn
pydantic-settings
optimum[onnxruntime]     - ONNX inference [RAG]
qdrant-client>=1.7.0     - Vector storage [RAG]
```

**External Connections**:
- ← DocumentService [HTTP] - Receives embedding requests
- → Qdrant [Vector Storage] - Store embeddings [RAG + MULTITENANT]

**API Endpoints**:
```
POST /embed              - Generate embeddings for text [RAG]
POST /embed/batch        - Batch embedding [RAG]
GET  /health             - Health check
```

---

### Tier 4 Statistics
```
Total Python Services: 2
├── [MULTITENANT]: 2 (100%)
└── [RAG]: 2 (100%)
```

---

## TIER 5: MESSAGE QUEUE

### Overview
RabbitMQ lam message broker, cho phep giao tiep bat dong bo giua .NET services va Python services.

---

### Package 5.1: RabbitMQ (Message Broker)

**Type**: RabbitMQ Server
**Version**: 3.x with Management Plugin
**Docker Ports**: 5672 (AMQP), 15672 (Management UI)

**Purpose**:
Decouples .NET services khoi Python services, cho phep xu ly RAG bat dong bo va scale independently.

**Multi-tenant**: [MULTITENANT]
- Messages chua TenantId trong payload de xu ly tenant-specific

**RAG**: [RAG]
- All RAG processing la async qua message queue

**Message Flows**:

1. **Chat Query Flow** [RAG]:
```
ChatService.API (.NET)
    ↓ Publish (MassTransit)
┌─────────────────────────┐
│  Exchange: chat_query   │
│  Queue: chat_query      │
└─────────────────────────┘
    ↓ Consume (aio-pika)
ChatProcessor (Python)
    ↓ Process RAG
    ↓ Publish response
┌─────────────────────────┐
│  Exchange: bot_response │
│  Queue: bot_response    │
└─────────────────────────┘
    ↓ Consume (MassTransit)
ChatService.BotResponseConsumer
    ↓ SignalR Broadcast
User receives response
```

2. **Message Schema** (example):
```json
{
  "conversationId": 123,
  "message": "What is Article 5?",
  "userId": 456,
  "tenantId": 1,
  "timestamp": "2026-01-11T10:00:00Z"
}
```

**Benefits**:
- **Async Processing**: User khong cho RAG processing (1-5 giay)
- **Resilience**: Neu Python service down, messages duoc queue
- **Scalability**: Co the scale Python services doc lap
- **Load Balancing**: Multiple consumers co the process cung queue

**Dependencies**:
- ← ChatService [Publish] - Chat messages
- → ChatProcessor [Consume] - Process messages
- ← ChatProcessor [Publish] - Bot responses
- → ChatService [Consume] - Receive responses

---

### Tier 5 Statistics
```
Total Message Brokers: 1
├── [MULTITENANT]: Yes (tenant in message payload)
└── [RAG]: Yes (core RAG communication)
```

---

## TIER 6: DATA & STORAGE

### Overview
Tang du lieu va luu tru, bao gom relational database, vector database, object storage, va LLM runtime.

---

### Package 6.1: SQL Server (Relational Database)

**Type**: Microsoft SQL Server 2022
**Docker Port**: 1433

**Purpose**: Luu tru relational data cho tat ca .NET microservices.

**Multi-tenant**: [MULTITENANT]
- **Isolation Strategy**: Row-level filtering
- **Implementation**: Moi table co cot TenantId (int)
- **Query Filtering**: TenancySpecification auto-filter theo TenantId
- **Interceptor**: UpdateTenancyInterceptor auto-set TenantId on save

**RAG**: N/A (chi luu metadata, khong luu vectors)

**Database**: `AIChat2025` (shared by all services)

**Key Tables with TenantId** [MULTITENANT]:
```sql
Accounts         (Id, TenantId, Email, PasswordHash, ...)
Tenants          (Id, Name, Subdomain, ...)  -- TenantId = 1 for admin
ChatConversations(Id, TenantId, UserId, Title, ...)
PromptConfigs    (Id, TenantId, SystemPrompt, ...)
Documents        (Id, TenantId, Title, FilePath, Status, ...)
```

**DbContexts**:
```
├── AccountDbContext   - Account, Role, Permission
├── TenantDbContext    - Tenant, TenantConfig
├── ChatDbContext      - ChatConversation, Message, PromptConfig
└── DocumentDbContext  - Document, DocumentChunk
```

**Dependencies**:
- ← All .NET Services [EF Core]

---

### Package 6.2: Qdrant (Vector Database)

**Type**: Qdrant Vector Database
**Version**: Latest
**Docker Port**: 6333

**Purpose**: Luu tru document embeddings cho semantic search trong RAG pipeline.

**Multi-tenant**: [MULTITENANT]
- **Isolation Strategy**: Tenant filter in payload
- **Implementation**: Vectors store tenant_id trong payload
- **Query Filtering**: Filter by tenant_id khi search

**RAG**: [RAG]
- **Vector Storage**: Stores embeddings (dimension depends on model)
- **Semantic Search**: ANN search voi HNSW index
- **Metadata**: Stores chunk info, document_id, tenant_id, page, etc.

**Vector Schema**:
```python
{
    "id": "uuid",
    "vector": [0.123, ...],  # N dimensions
    "payload": {
        "tenant_id": 1,           # [MULTITENANT]
        "document_id": 123,
        "chunk_text": "...",
        "page": 5,
        "hierarchy": {
            "chapter": "Chuong I",
            "section": "Muc 1",
            "article": "Dieu 5"
        }
    }
}
```

**Dependencies**:
- ← EmbeddingService [Write] - Store vectors
- ← ChatProcessor [Read] - Semantic search
- ← DocumentService [Client] - Vector operations

---

### Package 6.3: MinIO (Object Storage)

**Type**: MinIO Object Storage
**Version**: Latest
**Docker Ports**: 9000 (API), 9001 (Console)

**Purpose**: Luu tru PDF files (tai lieu goc).

**Multi-tenant**: [MULTITENANT]
- **Isolation Strategy**: Tenant prefix in object path
- **Implementation**: Files stored as `{tenant_id}/{document_id}.pdf`
- **Access Control**: Via presigned URLs

**RAG**: [RAG]
- **Document Storage**: Stores original PDF files
- **Access by AI**: EmbeddingService can read via DocumentService

**Bucket Structure**:
```
ai-chat-2025/
├── 1/                    # Tenant 1
│   ├── doc_001.pdf
│   └── doc_002.pdf
├── 2/                    # Tenant 2
│   └── doc_003.pdf
└── ...
```

**Configuration**:
```json
{
  "MinioEndpoint": "minio:9000",
  "MinioBucket": "ai-chat-2025",
  "MinioAccessKey": "admin",
  "MinioSecretKey": "password"
}
```

**Dependencies**:
- ← StorageService [Write/Read] - File operations

---

### Package 6.4: Ollama (LLM Runtime)

**Type**: Ollama LLM Runtime
**Version**: Latest
**Docker Port**: 11434

**Purpose**: Local LLM inference runtime cho RAG generation.

**Multi-tenant**: N/A
- Shared LLM runtime for all tenants
- Tenant-specific: Only prompt templates (stored in DB)

**RAG**: [RAG]
- **LLM Generation**: Generates answers based on retrieved context
- **Model**: Qwen or other models configured in Ollama
- **API**: OpenAI-compatible API (http://ollama:11434/v1/chat/completions)

**Configuration** (in ChatProcessor .env):
```
OLLAMA_BASE_URL=http://ollama:11434
```

**Dependencies**:
- ← ChatProcessor [HTTP API] - Generate responses

---

### Tier 6 Statistics
```
Total Data Stores: 4
├── SQL Server: 1 (5 DbContexts)
├── Vector DB: 1 (Qdrant)
├── Object Storage: 1 (MinIO)
└── LLM Runtime: 1 (Ollama)

[MULTITENANT] Stores: 3 (SQL, Qdrant, MinIO)
[RAG] Stores: 3 (Qdrant, MinIO, Ollama)
```

---

## CROSS-TIER DEPENDENCIES

### Multi-tenant Flow [MULTITENANT]

```
┌─────────────────────────────────────────────────────────────────┐
│                    MULTI-TENANT DATA FLOW                        │
└─────────────────────────────────────────────────────────────────┘

1. User Login:
   Frontend → ApiGateway → AccountService
                              ↓
                         Validate credentials
                              ↓
                         Generate JWT with TenantId claim
                              ↓
                         Return token to Frontend

2. Authenticated Request:
   Frontend (with JWT) → ApiGateway
                              ↓
                         Validate JWT
                              ↓
                         Extract TenantId from claims
                              ↓
                         Forward to Backend Service
                              ↓
   Backend Service ← CurrentTenantProvider.TenantId
                              ↓
                         TenancySpecification filters queries
                              ↓
                         Only tenant data returned

3. Data Isolation:
   SQL Server: WHERE TenantId = @CurrentTenantId
   Qdrant:     filter: {"tenant_id": 1}
   MinIO:      path: /{tenant_id}/file.pdf
```

### RAG Pipeline Flow [RAG]

```
┌─────────────────────────────────────────────────────────────────┐
│                      RAG PIPELINE FLOW                           │
└─────────────────────────────────────────────────────────────────┘

A. DOCUMENT INGESTION:
   ┌─────────────────────────────────────────────────────────────┐
   │ 1. Admin uploads PDF                                         │
   │    AdminCMS → ApiGateway → DocumentService                  │
   │                                  ↓                           │
   │ 2. Store file                                                │
   │    DocumentService → StorageService → MinIO                 │
   │                                  ↓                           │
   │ 3. Generate embeddings                                       │
   │    DocumentService → EmbeddingService                       │
   │                           ↓                                  │
   │ 4. Store vectors                                             │
   │    EmbeddingService → Qdrant                                │
   │                           ↓                                  │
   │ 5. Update document status                                    │
   │    DocumentService.Status = "Indexed"                       │
   └─────────────────────────────────────────────────────────────┘

B. CHAT QUERY:
   ┌─────────────────────────────────────────────────────────────┐
   │ 1. User sends message                                        │
   │    WebApp → SignalR Hub (ChatService)                       │
   │                   ↓                                          │
   │ 2. Save message & publish                                    │
   │    ChatService → DB (save message)                          │
   │    ChatService → RabbitMQ (publish query)                   │
   │                   ↓                                          │
   │ 3. Process RAG                                               │
   │    RabbitMQ → ChatProcessor                                 │
   │                   ↓                                          │
   │ 4. Hybrid search                                             │
   │    ChatProcessor → Qdrant (vector search)                   │
   │                   ↓                                          │
   │ 5. Generate response                                         │
   │    ChatProcessor → Ollama (LLM)                             │
   │                   ↓                                          │
   │ 6. Return response                                           │
   │    ChatProcessor → RabbitMQ (publish response)              │
   │                   ↓                                          │
   │ 7. Broadcast to user                                         │
   │    RabbitMQ → BotResponseConsumer → SignalR → WebApp        │
   └─────────────────────────────────────────────────────────────┘
```

---

## DIAGRAM LAYOUT (Text-based for Draw.io)

```
┌──────────────────────────────────────────────────────────────────────┐
│                   TIER 1: PRESENTATION LAYER                          │
│                                                                       │
│   ┌──────────────────────┐              ┌──────────────────────┐    │
│   │     WebApp           │              │     AdminCMS         │    │
│   │   (Tenant Portal)    │              │   (Admin Portal)     │    │
│   │   [MULTITENANT]      │              │   [MULTITENANT]      │    │
│   │   [RAG]              │              │   [RAG]              │    │
│   └──────────────────────┘              └──────────────────────┘    │
└──────────────────────────────────────────────────────────────────────┘
                    │ HTTPS                         │ HTTPS
                    └───────────────┬───────────────┘
                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│                   TIER 2: API GATEWAY                                 │
│                                                                       │
│   ┌────────────────────────────────────────────────────────────┐    │
│   │                    ApiGateway (YARP)                        │    │
│   │                    [MULTITENANT]                            │    │
│   │   Routes: /web-api/account, /tenant, /chat, /document, /storage │
│   └────────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────────┘
                                    │
            ┌───────────────────────┼───────────────────────┐
            │           │           │           │           │
            ▼           ▼           ▼           ▼           ▼
┌──────────────────────────────────────────────────────────────────────┐
│               TIER 3: APPLICATION SERVICES (.NET 9)                   │
│                                                                       │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐       │
│  │ Account    │ │ Tenant     │ │ Document   │ │ Storage    │       │
│  │ Service    │ │ Service    │ │ Service    │ │ Service    │       │
│  │ [MULTI]    │ │ [MULTI]    │ │ [M][RAG]   │ │ [MULTI]    │       │
│  └────────────┘ └────────────┘ └────────────┘ └────────────┘       │
│                                                                       │
│  ┌────────────┐ ┌──────────────────────────────────────────┐       │
│  │ Chat       │ │           Infrastructure                  │       │
│  │ Service    │ │           (Shared Library)                │       │
│  │ [M][RAG]   │ │           [MULTITENANT]                   │       │
│  │ [SignalR]  │ └──────────────────────────────────────────┘       │
│  └────────────┘                                                      │
└──────────────────────────────────────────────────────────────────────┘
         │                              │
         │ Async (MassTransit)          │ HTTP
         ▼                              ▼
┌──────────────────────────────────────────────────────────────────────┐
│                   TIER 5: MESSAGE QUEUE                               │
│                                                                       │
│   ┌────────────────────────────────────────────────────────────┐    │
│   │                      RabbitMQ                               │    │
│   │                      [RAG]                                  │    │
│   │   Queues: chat_query, bot_response                         │    │
│   └────────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────────┘
                    │
          ┌─────────┴─────────┐
          ▼                   ▼
┌──────────────────────────────────────────────────────────────────────┐
│               TIER 4: AI PROCESSING (Python 3.11)                     │
│                                                                       │
│   ┌──────────────────────┐        ┌──────────────────────┐         │
│   │   ChatProcessor      │        │  EmbeddingService    │         │
│   │   [MULTITENANT]      │        │  [MULTITENANT]       │         │
│   │   [RAG]              │        │  [RAG]               │         │
│   └──────────────────────┘        └──────────────────────┘         │
└──────────────────────────────────────────────────────────────────────┘
            │                              │
            │ Vector Search                │ Vector Store
            ▼                              ▼
┌──────────────────────────────────────────────────────────────────────┐
│                   TIER 6: DATA & STORAGE                              │
│                                                                       │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐       │
│  │ SQL Server │ │ Qdrant     │ │ MinIO      │ │ Ollama     │       │
│  │ (5 DBCtx)  │ │ Vector DB  │ │ Object     │ │ LLM        │       │
│  │ [MULTI]    │ │ [M][RAG]   │ │ [M][RAG]   │ │ [RAG]      │       │
│  └────────────┘ └────────────┘ └────────────┘ └────────────┘       │
└──────────────────────────────────────────────────────────────────────┘

LEGEND:
[MULTI] = [MULTITENANT]
[RAG]   = RAG-related
[M]     = [MULTITENANT] (abbreviated)
────▶   = Sync (HTTP/HTTPS)
═══▶    = Async (RabbitMQ/MassTransit)
```

---

## STATISTICS SUMMARY

### Package Counts
```
┌─────────────────────────────┬───────┬─────────────┬───────┐
│ TIER                        │ Total │ MULTITENANT │ RAG   │
├─────────────────────────────┼───────┼─────────────┼───────┤
│ 1. Presentation Layer       │   2   │     2       │   2   │
│ 2. API Gateway              │   1   │     1       │   0   │
│ 3. Application Services     │   6   │     6       │   2   │
│ 4. AI Processing            │   2   │     2       │   2   │
│ 5. Message Queue            │   1   │     1       │   1   │
│ 6. Data & Storage           │   4   │     3       │   3   │
├─────────────────────────────┼───────┼─────────────┼───────┤
│ TOTAL                       │  16   │    15       │  10   │
└─────────────────────────────┴───────┴─────────────┴───────┘
```

### Technology Summary
```
Frontend:        ASP.NET MVC (.NET 9)
API Gateway:     YARP Reverse Proxy (.NET 9)
Backend:         .NET 9 WebAPI + MassTransit + SignalR
AI Layer:        Python 3.11 + FastAPI + ONNX Runtime
Database:        SQL Server 2022 (shared) + Qdrant (vectors)
Storage:         MinIO (objects)
LLM:             Ollama (local inference)
Message Queue:   RabbitMQ (async communication)
Orchestration:   .NET Aspire 9.3 + Docker Compose
```

### Key Architectural Decisions
```
1. SHARED DATABASE: All .NET services use single AIChat2025 DB
   → Pro: Simple transactions, easy joins
   → Con: Coupling, scaling limitations

2. ROW-LEVEL TENANCY: TenantId column in all tenant tables
   → Pro: Simple implementation
   → Con: Risk of cross-tenant data leak if filter missed

3. ASYNC RAG: RabbitMQ between ChatService and ChatProcessor
   → Pro: Decoupled, scalable, resilient
   → Con: Added complexity, eventual consistency

4. SIGNALR FOR STREAMING: Real-time response delivery
   → Pro: True streaming UX
   → Con: WebSocket connection management
```

---

*Generated by Claude Code - Phase 2 Package Analysis*
