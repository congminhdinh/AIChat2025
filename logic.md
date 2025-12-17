# AIChat2025 System Architecture & Logic Documentation

**Version**: 2.0.0
**Last Updated**: 2025-12-17
**Primary Focus**: Multitenancy Data Management Strategy

---

## Table of Contents

1. [Core Architecture: Multitenancy Strategy](#1-core-architecture-multitenancy-strategy)
2. [System Architecture Overview](#2-system-architecture-overview)
3. [AI Workflows](#3-ai-workflows)
4. [Complete Data Flow Examples](#4-complete-data-flow-examples)

---

# 1. Core Architecture: Multitenancy Strategy

## 1.1 Overview

AIChat2025 implements a **shared-database, discriminator-column multitenancy architecture** with a hybrid approach:
- **SQL Databases**: Tenant isolation using `TenantId` column with automatic enforcement via EF Core interceptors
- **Vector Database (Qdrant)**: Tenant isolation using `tenant_id` metadata field with filter-based queries
- **Special Tenant**: `tenant_id=1` (SuperAdmin) serves as a shared legal base accessible to all tenants

### Key Characteristics
- **Automatic Tenant Assignment**: EF Core interceptor auto-assigns `TenantId` on entity save
- **Dual-Query RAG**: Separate queries for state law (tenant_id=1) and company rules (tenant_id=user)
- **JWT-Based Context Propagation**: Tenant ID embedded in JWT claims and propagated through all layers
- **No Query Filter on Read**: Explicit tenant filtering in application code (except soft-delete filter)

---

## 1.2 Tenant Identification & Authentication

### JWT Token Generation

**Location**: `Infrastructure/Authentication/TokenClaimsService.cs:13-40`

```csharp
public TokenResponseDto GetTokenAsync(int tenantId, int userId, string username,
    string scope, bool isAdmin)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Name, username),
        new Claim(AuthorizationConstants.TOKEN_CLAIMS_TENANT, tenantId.ToString()),
        new Claim(AuthorizationConstants.POLICY_ADMIN, isAdmin ? "True" : "False"),
        new Claim(AuthorizationConstants.TOKEN_CLAIMS_TYPE_SCOPE, scope)
    };
    // Generate JWT with 7-day expiration
}
```

**Key Points**:
- `AuthorizationConstants.TOKEN_CLAIMS_TENANT` contains the tenant ID
- Generated during login by AccountService
- Expires after 7 days
- Signed with HMAC SHA256

### Tenant Extraction from JWT

**Location**: `Infrastructure/Web/CurrentUserProvider.cs:40-50`

```csharp
private int GetTenantId()
{
    var tenantClaim = _httpContextAccessor?.HttpContext?.User?
        .FindFirstValue(AuthorizationConstants.TOKEN_CLAIMS_TENANT);

    if (int.TryParse(tenantClaim, out int tenantId))
        return tenantId;

    return 0;
}
```

**Usage**:
```csharp
public class MyService
{
    private readonly ICurrentUserProvider _currentUser;

    public async Task DoWork()
    {
        int tenantId = _currentUser.TenantId;  // Extracted from JWT
        int userId = _currentUser.UserId;
        bool isAdmin = _currentUser.IsAdmin;
    }
}
```

---

## 1.3 Data Isolation in SQL Databases

### Entity Base Classes

**Location**: `Infrastructure/Entities/BaseEntity.cs`

```csharp
public abstract class BaseEntity
{
    public virtual int Id { get; set; }
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; }  // Soft delete
}

public abstract class TenancyEntity : AuditableEntity
{
    public int TenantId { get; set; }  // ← Tenant isolation column
}
```

**Inheritance Hierarchy**:
```
BaseEntity (Id)
    └── AuditableEntity (Audit fields + IsDeleted)
            └── TenancyEntity (TenantId)
```

### Tenant Entities

All tenant-scoped entities inherit from `TenancyEntity`:

| Entity | Service | Table | Tenant Field |
|--------|---------|-------|--------------|
| `Account` | AccountService | Accounts | TenantId |
| `ChatConversation` | ChatService | ChatConversations | TenantId |
| `ChatMessage` | ChatService | ChatMessages | TenantId |
| `PromptDocument` | DocumentService | PromptDocuments | TenantId |

**Note**: `Tenant` entity itself does NOT inherit from `TenancyEntity` (it's the master list).

### Automatic Tenant Assignment Interceptor

**Location**: `Infrastructure/Database/UpdateTenancyInterceptor.cs:9-28`

```csharp
public sealed class UpdateTenancyInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        var entries = context.ChangeTracker.Entries<TenancyEntity>();
        var tenantProvider = GetService<ICurrentUserProvider>();

        // SuperAdmin (tenant_id=1) can create entities for other tenants
        if (tenantProvider.TenantId != 1)
        {
            foreach (var entry in entries)
            {
                entry.Entity.TenantId = tenantProvider.TenantId;
            }
        }

        return base.SavingChangesAsync(eventData, result);
    }
}
```

**Behavior**:
- **Trigger**: Executes on every `SaveChangesAsync()` call
- **Target**: All entities inheriting from `TenancyEntity`
- **Action**: Auto-assigns `TenantId` from `CurrentUserProvider`
- **Exception**: SuperAdmin (tenant_id=1) can manually set `TenantId` for cross-tenant operations

### Soft Delete Query Filter

**Location**: `Infrastructure/Database/BaseDbContext.cs`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Global query filter for soft deletes
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
        {
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
        }
    }
}
```

**Behavior**:
- Automatically excludes records where `IsDeleted = true`
- Applied globally to all `AuditableEntity` descendants
- No explicit tenant filter at database level (handled in application code)

---

## 1.4 Data Isolation in Vector Database (Qdrant)

### Vector Storage with Tenant Metadata

**Location**: `Services/EmbeddingService/src/business.py:79-105`

When documents are vectorized, the `tenant_id` is stored in the payload:

```python
def vectorize_batch(self, items: list, collection_name: str = None):
    for item in items:
        embedding = self.encode_text(item.text)  # 768-dim vector
        point_id = str(uuid.uuid4())

        points.append(PointStruct(
            id=point_id,
            vector=embedding,
            payload={
                "text": item.text,
                "tenant_id": item.metadata["tenant_id"],  # ← Tenant isolation
                "source_id": item.metadata["source_id"],
                "file_name": item.metadata["file_name"],
                "heading1": item.metadata["heading1"],
                "heading2": item.metadata["heading2"],
                "type": item.metadata["type"]
            }
        ))

    self.qdrant_client.upsert(collection_name=collection_name, points=points)
```

**Vectorization Flow**:
```
DocumentService (.NET)
    ↓ (Hangfire Background Job)
VectorizeBackgroundJob.ProcessBatch(chunks, tenantId)
    ↓ (HTTP POST /vectorize-batch)
EmbeddingService (Python)
    ↓ (Generate 768-dim embeddings)
Qdrant Database (Store with tenant_id in payload)
```

### Dual-Query Retrieval Strategy

**Location**: `Services/ChatProcessor/src/business.py:149-175`

The chat processor uses a **dual-query architecture** to guarantee cross-referencing:

```python
async def process_chat_message(conversation_id, user_id, message, tenant_id,
                                ollama_service, qdrant_service):
    # Step 1: Generate query embedding
    query_embedding = qdrant_service.get_embedding(message)

    # Step 2: Execute parallel dual queries
    legal_base_task = qdrant_service.search_exact_tenant(
        query_vector=query_embedding,
        tenant_id=1,        # ← State law (shared)
        limit=1
    )
    company_rule_task = qdrant_service.search_exact_tenant(
        query_vector=query_embedding,
        tenant_id=tenant_id,  # ← Company-specific
        limit=1
    )

    # Execute both in parallel
    legal_base_results, company_rule_results = await asyncio.gather(
        legal_base_task, company_rule_task, return_exceptions=True
    )

    # Step 3: Build labeled context
    context_sections = []
    if legal_base_results:
        context_sections.append(f"[STATE LAW]\n{legal_base_results[0].payload['text']}")
    if company_rule_results:
        context_sections.append(f"[COMPANY REGULATION]\n{company_rule_results[0].payload['text']}")
```

### Exact Tenant Search Implementation

**Location**: `Services/ChatProcessor/src/business.py:108-122`

```python
async def search_exact_tenant(self, query_vector: List[float],
                               tenant_id: int, limit: int = 1):
    """Search for documents with exact tenant_id match"""
    search_filter = Filter(
        must=[  # ← Exact match, not OR
            FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id))
        ]
    )
    results = self.client.search(
        collection_name=self.collection_name,
        query_vector=query_vector,
        query_filter=search_filter,
        limit=limit
    )
    return results
```

**Why Dual Queries Instead of OR Filter?**

| Approach | Filter Logic | Guarantee | Problem |
|----------|-------------|-----------|---------|
| **Old (OR)** | `tenant_id=1 OR tenant_id=user` | No | Vector similarity could return 2×law or 2×company |
| **New (Dual)** | `tenant_id=1 (query A)` + `tenant_id=user (query B)` | Yes | Always attempts both sources |

**Benefits**:
1. **Guaranteed Cross-Reference**: Always retrieves from both legal base and company rules
2. **No Vector Bias**: Similarity scores can't cause one source to dominate results
3. **Compliance Focus**: Ensures LLM can compare company rules against legal requirements
4. **Clear Attribution**: Each document labeled with its source type (`[STATE LAW]` / `[COMPANY REGULATION]`)

### Vector Deletion with Tenant Filter

**Location**: `Services/EmbeddingService/src/business.py:107-122`

```python
def delete_by_filter(self, source_id: int, tenant_id: int, type: int,
                     collection_name: str = None):
    delete_filter = Filter(
        must=[
            FieldCondition(key="source_id", match=MatchValue(value=source_id)),
            FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id)),
            FieldCondition(key="type", match=MatchValue(value=type))
        ]
    )

    self.qdrant_client.delete(
        collection_name=collection_name,
        points_selector=FilterSelector(filter=delete_filter)
    )
```

**Safety**: Tenant ID is required for deletion to prevent cross-tenant data removal.

---

## 1.5 Context Propagation Flow

### Full Request Flow with Tenant Context

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. CLIENT REQUEST                                               │
│    Authorization: Bearer <JWT with tenant_id claim>            │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. API GATEWAY (YARP - Port 5000)                              │
│    - Routes to backend service                                  │
│    - Forwards Authorization header                              │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. BACKEND SERVICE (.NET Microservice)                          │
│    - JWT Authentication Middleware validates token              │
│    - CurrentUserProvider extracts tenant_id from claims         │
│    - ICurrentUserProvider.TenantId available in all services    │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4A. SQL DATABASE WRITE                                          │
│    - UpdateTenancyInterceptor reads CurrentUserProvider.TenantId│
│    - Auto-assigns TenantId to TenancyEntity on SaveChanges      │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 4B. VECTOR DATABASE WRITE (via EmbeddingService)                │
│    - DocumentService passes tenant_id in HTTP request body      │
│    - EmbeddingService stores tenant_id in vector payload        │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 4C. CHAT PROCESSING (via RabbitMQ)                             │
│    - ChatService publishes tenant_id in UserPromptReceivedEvent │
│    - ChatProcessor receives tenant_id in message                │
│    - Dual-query searches with tenant_id=1 and tenant_id=user    │
└─────────────────────────────────────────────────────────────────┘
```

### Tenant Context in Different Scenarios

#### Scenario 1: REST API Call
```
User → API Gateway → AccountService
    JWT (tenant_id=2) → CurrentUserProvider.TenantId = 2
        → Repository.Add(entity) → UpdateTenancyInterceptor sets entity.TenantId = 2
```

#### Scenario 2: Document Vectorization
```
User → API Gateway → DocumentService
    JWT (tenant_id=2) → CurrentUserProvider.TenantId = 2
        → Hangfire Job → VectorizeBackgroundJob.ProcessBatch(chunks, tenantId=2)
            → HTTP POST to EmbeddingService with metadata: { "tenant_id": 2 }
                → Qdrant stores vector with payload.tenant_id = 2
```

#### Scenario 3: Chat with RAG
```
User → SignalR Hub → ChatService (tenant_id=2 from JWT)
    → RabbitMQ: UserPromptReceivedEvent { tenant_id: 2 }
        → ChatProcessor (Python) receives message
            → Query A: search_exact_tenant(tenant_id=1)  # State law
            → Query B: search_exact_tenant(tenant_id=2)  # Company rules
                → LLM generates response with both contexts
```

---

## 1.6 Special Case: SuperAdmin Tenant (tenant_id=1)

### Purpose
- **Shared Legal Base**: Documents uploaded to tenant_id=1 are accessible to all tenants
- **State Law Repository**: Contains Vietnamese labor law, regulations, and compliance documents
- **Cross-Tenant Administration**: SuperAdmin can create entities for other tenants

### Implementation Details

**Tenant Table Seed Data**:
```csharp
// Location: TenantService/Migrations
migrationBuilder.InsertData(
    table: "Tenants",
    columns: new[] { "Id", "Name", "IsActive" },
    values: new object[] { 1, "SuperAdmin", true }
);
```

**Interceptor Exception**:
```csharp
// UpdateTenancyInterceptor.cs:19-25
if (tenantProvider.TenantId != 1)  // ← SuperAdmin bypass
{
    foreach (var entry in entries)
    {
        entry.Entity.TenantId = tenantProvider.TenantId;
    }
}
```

**Dual-Query Strategy**:
```python
# ChatProcessor always queries tenant_id=1 for state law
legal_base_results = search_exact_tenant(tenant_id=1)  # Accessible to all
company_rule_results = search_exact_tenant(tenant_id=user_tenant)  # Tenant-specific
```

### Access Pattern

| User Tenant | Can Access tenant_id=1? | Can Access tenant_id=2? | Can Access tenant_id=3? |
|-------------|-------------------------|-------------------------|-------------------------|
| tenant_id=1 (SuperAdmin) | Yes | Yes (if explicit) | Yes (if explicit) |
| tenant_id=2 | Yes (via dual-query) | Yes | No |
| tenant_id=3 | Yes (via dual-query) | No | Yes |

---

# 2. System Architecture Overview

## 2.1 Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CLIENT APPLICATION                           │
│                    (Browser / Mobile App)                            │
└────────────────────────────┬────────────────────────────────────────┘
                             │ HTTPS (JWT Token)
                             ↓
┌─────────────────────────────────────────────────────────────────────┐
│                      API GATEWAY (YARP)                              │
│                         Port: 5000                                   │
│  Routes:                                                             │
│    /web-api/account/**   → AccountService (5050)                    │
│    /web-api/tenant/**    → TenantService (5062)                     │
│    /web-api/document/**  → DocumentService (5165)                   │
│    /web-api/storage/**   → StorageService (5113)                    │
│    /web-api/chat/**      → ChatService (5218)                       │
│    /hubs/chat/**         → ChatService SignalR (5218)               │
└────────────────────────────┬────────────────────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ↓                    ↓                    ↓
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│  Account     │   │   Tenant     │   │  Document    │
│  Service     │   │   Service    │   │  Service     │
│  (Port 5050) │   │  (Port 5062) │   │  (Port 5165) │
│              │   │              │   │              │
│  SQL Server  │   │  SQL Server  │   │  SQL Server  │
│  Database    │   │  Database    │   │  Database    │
└──────────────┘   └──────────────┘   └──────┬───────┘
                                              │
                         ┌────────────────────┤
                         │                    │
                         ↓                    ↓
                 ┌──────────────┐   ┌──────────────┐
                 │   Storage    │   │   Hangfire   │
                 │   Service    │   │  Background  │
                 │  (Port 5113) │   │    Jobs      │
                 └──────────────┘   └──────┬───────┘
                                           │
                                           ↓
        ┌──────────────────────────────────────────────┐
        │         EMBEDDING SERVICE (Python)            │
        │              Port: 8000                       │
        │  Model: truro7/vn-law-embedding (768-dim)    │
        └──────────────────┬───────────────────────────┘
                           │
                           ↓
        ┌──────────────────────────────────────────────┐
        │         QDRANT VECTOR DATABASE                │
        │              Port: 6333                       │
        │  Collection: vn_law_documents                 │
        │  Vectors: 768-dim with tenant_id metadata    │
        └───────────────────────────────────────────────┘

┌──────────────┐                              ┌──────────────┐
│    Chat      │   ←── RabbitMQ ───────────→  │    Chat      │
│   Service    │       (Port 5672)            │  Processor   │
│  (Port 5218) │                              │  (Port 8001) │
│              │  UserPromptReceived          │   (Python)   │
│  SQL Server  │  BotResponseCreated          │              │
│  SignalR Hub │                              └──────┬───────┘
└──────────────┘                                     │
                                                     ↓
                                        ┌─────────────────────┐
                                        │   OLLAMA LLM        │
                                        │   Port: 11434       │
                                        │   ontocord/vistral  │
                                        └─────────────────────┘
```

---

## 2.2 Infrastructure Layer

**Location**: `D:\Project\AIChat2025\Infrastructure\`

Provides shared cross-cutting concerns for all .NET microservices.

### Key Components

| Component | Purpose | Key Files |
|-----------|---------|-----------|
| **Authentication** | JWT generation & validation | `TokenClaimsService.cs`, `AuthorizationConstants.cs` |
| **Database** | EF Core base classes & interceptors | `BaseDbContext.cs`, `UpdateTenancyInterceptor.cs`, `UpdateAuditableInterceptor.cs` |
| **Entities** | Base entity classes | `BaseEntity.cs` (BaseEntity, AuditableEntity, TenancyEntity) |
| **Tenancy** | Tenant context provider | `CurrentTenantProvider.cs` |
| **Web** | HTTP request handling | `CurrentUserProvider.cs` |
| **Specifications** | Repository pattern | Ardalis.Specification implementation |
| **Logging** | Serilog configuration | `AppLogger.cs` |

### Dependency Injection Setup

```csharp
// Infrastructure/Extensions.cs
public static IServiceCollection AddInfrastructure(this IServiceCollection services)
{
    services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
    services.AddScoped<ICurrentTenantProvider, CurrentTenantProvider>();
    services.AddScoped<ITokenClaimsService, TokenClaimsService>();
    services.AddScoped<UpdateTenancyInterceptor>();
    services.AddScoped<UpdateAuditableInterceptor>();
    // ...
}
```

---

## 2.3 AppHost (.NET Aspire Orchestration)

**Location**: `D:\Project\AIChat2025\AppHost\Program.cs`

.NET Aspire orchestration host that manages service lifecycle and dependencies.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var accountService = builder.AddProject<AccountService>("accountservice");
var tenantService = builder.AddProject<TenantService>("tenantservice");
var documentService = builder.AddProject<DocumentService>("documentservice");
var storageService = builder.AddProject<StorageService>("storageservice");
var chatService = builder.AddProject<ChatService>("chatservice");

builder.AddProject<ApiGateway>("apigateway")
    .WithReference(accountService)
    .WithReference(tenantService)
    .WithReference(documentService)
    .WithReference(storageService)
    .WithReference(chatService);

builder.Build().Run();
```

**Features**:
- Service discovery
- Automatic port management
- Health monitoring
- Unified logging

---

## 2.4 API Gateway (YARP)

**Location**: `D:\Project\AIChat2025\ApiGateway\Program.cs`

YARP (Yet Another Reverse Proxy) provides unified API gateway with Swagger aggregation.

### Routing Configuration

```json
{
  "ReverseProxy": {
    "Routes": {
      "account-route": {
        "ClusterId": "account-cluster",
        "Match": { "Path": "/web-api/account/{**catch-all}" }
      },
      "tenant-route": {
        "ClusterId": "tenant-cluster",
        "Match": { "Path": "/web-api/tenant/{**catch-all}" }
      }
    },
    "Clusters": {
      "account-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://localhost:5050" }
        }
      }
    }
  }
}
```

### Features

- **Reverse Proxy**: Routes requests to backend services
- **Swagger Aggregation**: Combines all service Swagger docs into unified UI
- **Security Injection**: Auto-injects Bearer token requirements in Swagger
- **CORS**: Enabled for all origins
- **No Authentication**: Gateway forwards JWT to backend services for validation

---

## 2.5 Microservices

### AccountService (Port 5050)

**Purpose**: User authentication, registration, and account management

**Database**: AccountDbContext (SQL Server)

**Key Entities**:
```csharp
public class Account : TenancyEntity
{
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public bool IsAdmin { get; set; }
    public bool TenancyActive { get; set; }
    public int TenantId { get; set; }  // Inherited from TenancyEntity
}
```

**Key Endpoints**:
- `POST /web-api/account/login` - Generate JWT token
- `POST /web-api/account/register` - Create new account
- `GET /web-api/account/me` - Get current user info

**Query Filter**: `HasQueryFilter(a => a.TenancyActive && a.IsActive && !a.IsDeleted)`

---

### TenantService (Port 5062)

**Purpose**: Tenant management and administration

**Database**: TenantDbContext (SQL Server)

**Key Entities**:
```csharp
public class Tenant : AuditableEntity  // NOT TenancyEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? Permissions { get; set; }
}
```

**Seed Data**: SuperAdmin tenant (Id=1) pre-seeded in migrations

**Key Endpoints**:
- `GET /web-api/tenant` - List all tenants
- `POST /web-api/tenant` - Create new tenant
- `PUT /web-api/tenant/{id}` - Update tenant
- `DELETE /web-api/tenant/{id}` - Soft-delete tenant

---

### DocumentService (Port 5165)

**Purpose**: Document upload, processing, and vectorization

**Database**: DocumentDbContext (SQL Server) + Hangfire

**Key Entities**:
```csharp
public class PromptDocument : TenancyEntity
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public DocumentAction Action { get; set; }  // Upload, Vectorize_Start, Vectorize_Success
    public int TenantId { get; set; }
}
```

**Key Endpoints**:
- `POST /web-api/document/upload` - Upload .docx file
- `POST /web-api/document/vectorize/{id}` - Start vectorization
- `GET /web-api/document` - List documents
- `DELETE /web-api/document/{id}` - Delete document and vectors

**Background Processing**: Uses Hangfire for asynchronous vectorization

---

### ChatService (Port 5218)

**Purpose**: Real-time chat with SignalR and message persistence

**Database**: ChatDbContext (SQL Server)

**Key Entities**:
```csharp
public class ChatConversation : TenancyEntity
{
    public int UserId { get; set; }
    public string? Title { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public ICollection<ChatMessage> Messages { get; set; }
}

public class ChatMessage : TenancyEntity
{
    public int ConversationId { get; set; }
    public string Message { get; set; }
    public MessageRole Role { get; set; }  // User, Assistant
    public string? ModelUsed { get; set; }
}
```

**Key Components**:
- **SignalR Hub**: Real-time WebSocket communication
- **RabbitMQ Publisher**: Publishes `UserPromptReceivedEvent`
- **RabbitMQ Consumer**: Consumes `BotResponseCreatedEvent`

**Message Flow**:
```
User → SignalR Hub → Save to DB → Publish to RabbitMQ → Broadcast to clients
ChatProcessor → RabbitMQ → ChatService Consumer → Save to DB → Broadcast to clients
```

---

### StorageService (Port 5113)

**Purpose**: File storage management (uploads/downloads)

**No Database**: File-based service

**Key Endpoints**:
- `POST /web-api/storage/upload-file` - Upload file to disk
- `GET /web-api/storage/download-file?filePath={path}` - Download file

**Storage Path**: `D:\Project\AIChat2025\Services\StorageService\Uploads\`

---

## 2.6 External Dependencies

### RabbitMQ (Port 5672)

**Purpose**: Message broker for async communication

**Configuration**:
- Username: guest
- Password: guest
- Virtual Host: /

**Queues**:
- `UserPromptReceived` - ChatService → ChatProcessor
- `BotResponseCreated` - ChatProcessor → ChatService

**Technology**: MassTransit for .NET integration

---

### SQL Server Databases

Each microservice has its own isolated database:

| Service | Database Name | Connection String Key |
|---------|---------------|----------------------|
| AccountService | AccountDb | "AccountDbContext" |
| TenantService | TenantDb | "TenantDbContext" |
| DocumentService | DocumentDb | "DocumentDbContext" |
| ChatService | ChatDb | "ChatDbContext" |

**Provider**: Microsoft SQL Server
**Migrations**: Code-First EF Core migrations

---

# 3. AI Workflows

## 3.1 Document Vectorization Pipeline

### Overview

Document vectorization converts .docx files into searchable vector embeddings with hierarchical structure preservation.

### Full Pipeline Flow

```
┌──────────────┐
│ User uploads │
│ .docx file   │
└──────┬───────┘
       │ POST /web-api/document/upload
       ↓
┌─────────────────────────────────────────────────────────────┐
│ DocumentService                                             │
│ 1. Save metadata to DB (Action: Upload)                    │
│ 2. Upload file to StorageService                           │
│ 3. Status: Standardization                                 │
└──────┬──────────────────────────────────────────────────────┘
       │ POST /web-api/document/vectorize/{id}
       ↓
┌─────────────────────────────────────────────────────────────┐
│ DocumentService - Vectorization                             │
│ 1. Update status: Vectorize_Start                          │
│ 2. Download .docx from StorageService                      │
│ 3. Extract hierarchical chunks:                            │
│    - Heading1: CHƯƠNG I (Chapter)                          │
│    - Heading2: Mục 1 (Section)                             │
│    - Heading3: Điều 1 (Article)                            │
│    - Content: Body paragraphs                              │
│ 4. Create batches (10 chunks/batch)                       │
│ 5. Enqueue Hangfire jobs                                   │
│ 6. Update status: Vectorize_Success                        │
└──────┬──────────────────────────────────────────────────────┘
       │ Hangfire Background Job
       ↓
┌─────────────────────────────────────────────────────────────┐
│ VectorizeBackgroundJob.ProcessBatch                         │
│ 1. Build batch request with metadata:                      │
│    - text: FullText (all hierarchical levels)             │
│    - metadata:                                             │
│        * tenant_id: User's tenant                          │
│        * source_id: Document ID                            │
│        * file_name: Original filename                      │
│        * heading1, heading2: Hierarchical context          │
│        * type: 1 (document type)                           │
└──────┬──────────────────────────────────────────────────────┘
       │ POST /vectorize-batch
       ↓
┌─────────────────────────────────────────────────────────────┐
│ EmbeddingService (Python - Port 8000)                       │
│ 1. Load model: truro7/vn-law-embedding                     │
│ 2. For each chunk:                                          │
│    - Tokenize text (max 512 tokens)                        │
│    - Generate embedding (768 dimensions)                   │
│    - Mean pooling + L2 normalization                       │
│ 3. Create Qdrant points with payload                       │
└──────┬──────────────────────────────────────────────────────┘
       │ Upsert to Qdrant
       ↓
┌─────────────────────────────────────────────────────────────┐
│ Qdrant Vector Database (Port 6333)                         │
│ Collection: vn_law_documents                                │
│ Point Structure:                                            │
│ {                                                           │
│   id: "uuid",                                              │
│   vector: [0.123, -0.456, ...],  // 768 dimensions        │
│   payload: {                                               │
│     text: "Full hierarchical text",                        │
│     tenant_id: 2,                                          │
│     source_id: 123,                                        │
│     heading1: "CHƯƠNG I: QUY ĐỊNH CHUNG",                 │
│     heading2: "Mục 1: Phạm vi điều chỉnh",                │
│     file_name: "labor-law.docx"                            │
│   }                                                         │
│ }                                                           │
└─────────────────────────────────────────────────────────────┘
```

### Hierarchical Chunking

**Location**: `Services/DocumentService/Features/PromptDocumentBusiness.cs:395-460`

**Regex Patterns**:
```csharp
private readonly Regex _regexHeading1 = new(@"^CHƯƠNG\s+[IVXLCDM]+");  // Chapter
private readonly Regex _regexHeading2 = new(@"^Mục\s+\d+");            // Section
private readonly Regex _regexHeading3 = new(@"^Điều\s+\d+");           // Article
```

**Extraction Logic**:
1. Read .docx paragraphs sequentially
2. Match against regex patterns to identify heading levels
3. When new heading found, flush previous chunk
4. Accumulate content under current article (Điều)
5. Preserve hierarchical context in each chunk

**Example Chunk**:
```json
{
  "Heading1": "CHƯƠNG I: QUY ĐỊNH CHUNG",
  "Heading2": "Mục 1: Phạm vi điều chỉnh",
  "Content": "Điều 1. Định nghĩa\nHợp đồng lao động là...",
  "FullText": "CHƯƠNG I: QUY ĐỊNH CHUNG\nMục 1: Phạm vi điều chỉnh\nĐiều 1. Định nghĩa\nHợp đồng lao động là...",
  "DocumentId": 123,
  "FileName": "labor-law.docx"
}
```

### Embedding Model

**Model**: `truro7/vn-law-embedding`
**Type**: Sentence-Transformers (BERT-based)
**Dimensions**: 768
**Optimized For**: Vietnamese legal text
**Technology**: ONNX Runtime (optimized inference)

**Embedding Process**:
```python
def encode_text(self, text: str) -> List[float]:
    # Tokenize
    encoded_input = self.tokenizer(text, padding=True, truncation=True,
                                    return_tensors='pt', max_length=512)
    # Forward pass
    model_output = self.model(**encoded_input)
    # Mean pooling
    sentence_embeddings = self.mean_pooling(model_output, encoded_input['attention_mask'])
    # L2 normalize
    sentence_embeddings = torch.nn.functional.normalize(sentence_embeddings, p=2, dim=1)
    return sentence_embeddings[0].tolist()  # 768 floats
```

---

## 3.2 Chat Processing with Dual-Query RAG

### Overview

ChatProcessor implements a **dual-query Retrieval-Augmented Generation (RAG)** architecture that guarantees cross-referencing between state law and company regulations.

### Complete Chat Flow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. USER SENDS MESSAGE                                       │
│    Client → SignalR Hub → ChatService                       │
└──────┬──────────────────────────────────────────────────────┘
       │
       ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. CHATSERVICE PROCESSING                                    │
│    - Save user message to ChatMessages table                │
│    - Broadcast message to SignalR clients                   │
│    - Publish RabbitMQ event: UserPromptReceivedEvent        │
│      {                                                       │
│        conversation_id: 1,                                  │
│        message: "What are overtime rules?",                 │
│        user_id: 123,                                        │
│        tenant_id: 2  ← From JWT                            │
│      }                                                       │
└──────┬──────────────────────────────────────────────────────┘
       │ RabbitMQ Queue
       ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. CHATPROCESSOR (Python - Port 8001)                       │
│    RabbitMQ Consumer receives message                       │
└──────┬──────────────────────────────────────────────────────┘
       │
       ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. GENERATE QUERY EMBEDDING                                  │
│    query_embedding = qdrant_service.get_embedding(message)  │
│    ↓ POST /embed to EmbeddingService                        │
│    ← Returns 768-dim vector                                 │
└──────┬──────────────────────────────────────────────────────┘
       │
       ↓
┌─────────────────────────────────────────────────────────────┐
│ 5. DUAL-QUERY RAG RETRIEVAL (Parallel Execution)            │
│                                                              │
│    Query A: Legal Base                                      │
│    ┌────────────────────────────────────────┐              │
│    │ search_exact_tenant(                   │              │
│    │   query_vector=query_embedding,        │              │
│    │   tenant_id=1,  ← State law (shared)   │              │
│    │   limit=1                              │              │
│    │ )                                      │              │
│    └────────────────────────────────────────┘              │
│                                                              │
│    Query B: Company Rules                                   │
│    ┌────────────────────────────────────────┐              │
│    │ search_exact_tenant(                   │              │
│    │   query_vector=query_embedding,        │              │
│    │   tenant_id=2,  ← User's tenant        │              │
│    │   limit=1                              │              │
│    │ )                                      │              │
│    └────────────────────────────────────────┘              │
│                                                              │
│    ↓ await asyncio.gather(query_a, query_b)                │
│                                                              │
│    Results:                                                  │
│    - legal_base_results: [vector with tenant_id=1]         │
│    - company_rule_results: [vector with tenant_id=2]       │
└──────┬──────────────────────────────────────────────────────┘
       │
       ↓
┌─────────────────────────────────────────────────────────────┐
│ 6. BUILD LABELED CONTEXT                                     │
│    context_sections = []                                    │
│                                                              │
│    if legal_base_results:                                   │
│        context_sections.append(                             │
│            "[STATE LAW]\n" + result.payload['text']        │
│        )                                                     │
│                                                              │
│    if company_rule_results:                                 │
│        context_sections.append(                             │
│            "[COMPANY REGULATION]\n" + result.payload['text']│
│        )                                                     │
│                                                              │
│    enhanced_prompt = f"""                                   │
│    Context information:                                     │
│    {context}                                                │
│                                                              │
│    User question: {message}                                 │
│                                                              │
│    Please answer based on the context provided above.       │
│    If both STATE LAW and COMPANY REGULATION are provided,   │
│    compare and contrast them in your response.              │
│    """                                                       │
└──────┬──────────────────────────────────────────────────────┘
       │
       ↓
┌─────────────────────────────────────────────────────────────┐
│ 7. LLM GENERATION                                            │
│    ai_response = await ollama_service.generate_response(    │
│        prompt=enhanced_prompt,                              │
│        conversation_history=None  ← Stateless               │
│    )                                                         │
│    ↓ POST /api/chat to Ollama (Port 11434)                 │
│    Model: ontocord/vistral:latest (Vietnamese 7B LLM)       │
│    ← Returns AI-generated response                          │
└──────┬──────────────────────────────────────────────────────┘
       │
       ↓
┌─────────────────────────────────────────────────────────────┐
│ 8. PUBLISH RESPONSE                                          │
│    Publish RabbitMQ event: BotResponseCreatedEvent          │
│    {                                                         │
│        conversation_id: 1,                                  │
│        message: "According to Vietnamese labor law...",     │
│        user_id: 0,                                          │
│        model_used: "ontocord/vistral:latest",               │
│        rag_documents_used: 2,                               │
│        source_ids: ["doc-123", "doc-456"]                   │
│    }                                                         │
└──────┬──────────────────────────────────────────────────────┘
       │ RabbitMQ Queue
       ↓
┌─────────────────────────────────────────────────────────────┐
│ 9. CHATSERVICE RECEIVES RESPONSE                             │
│    - Save bot message to ChatMessages table                 │
│    - Broadcast message to SignalR clients                   │
│    - User sees response in real-time                        │
└─────────────────────────────────────────────────────────────┘
```

### Key Features

#### Dual-Query Architecture

**Motivation**: The original single-query approach using OR filter (`tenant_id=1 OR tenant_id=user`) could not guarantee retrieval from both sources due to vector similarity bias.

**Solution**: Execute two separate queries in parallel with exact match filters.

**Implementation**:
```python
# Query A: State Law (must match tenant_id=1)
search_filter_a = Filter(
    must=[FieldCondition(key="tenant_id", match=MatchValue(value=1))]
)

# Query B: Company Rules (must match tenant_id=user)
search_filter_b = Filter(
    must=[FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id))]
)

# Execute in parallel
results_a, results_b = await asyncio.gather(query_a, query_b)
```

**Benefits**:
1. Guaranteed cross-reference when documents exist in both sources
2. No vector similarity bias toward one source
3. Clear source attribution with labels
4. Better compliance checking
5. Improved answer quality for legal/regulation queries

#### Labeled Context

**Purpose**: Clearly distinguish between state law and company regulations in the LLM prompt.

**Format**:
```
Context information:
[STATE LAW]
Điều 98. Làm thêm giờ
1. Làm thêm giờ là thời gian làm việc vượt quá...
2. Người sử dụng lao động trả lương:
   a) Ngày thường: ít nhất 150%
   b) Ngày nghỉ: ít nhất 200%
   c) Ngày lễ: ít nhất 300%

[COMPANY REGULATION]
Quy định về tăng ca
1. Nhân viên làm thêm giờ được hưởng:
   - Ngày thường: 160%
   - Cuối tuần: 220%
   - Lễ tết: 350%

User question: What are the overtime compensation rules?

Please answer based on the context provided above.
If both STATE LAW and COMPANY REGULATION are provided,
compare and contrast them in your response.
```

**LLM Response Example**:
```
Về quy định tăng ca:

**Theo luật lao động (Điều 98):**
- Ngày thường: tối thiểu 150% lương
- Ngày nghỉ hàng tuần: tối thiểu 200% lương
- Ngày lễ, tết: tối thiểu 300% lương

**Quy định của công ty:**
- Ngày thường: 160% (cao hơn mức tối thiểu)
- Cuối tuần: 220% (cao hơn mức tối thiểu)
- Lễ tết: 350% (cao hơn mức tối thiểu)

**Kết luận:** Công ty đang áp dụng mức cao hơn quy định pháp luật,
đảm bảo tuân thủ và có lợi cho người lao động.
```

#### Stateless Processing

- Each message processed independently
- No conversation history maintained
- No context from previous messages
- Simplifies scaling and reduces memory usage

---

## 3.3 EmbeddingService (Python)

**Location**: `Services/EmbeddingService/`
**Port**: 8000
**Model**: truro7/vn-law-embedding
**Framework**: FastAPI + ONNX Runtime

### API Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/embed` | POST | Generate embedding for single text |
| `/vectorize` | POST | Generate embedding + store in Qdrant |
| `/vectorize-batch` | POST | Batch vectorize + store (used by DocumentService) |
| `/api/embeddings/delete` | DELETE | Delete vectors by filter (tenant_id, source_id, type) |
| `/health` | GET | Health check |

### Core Service

```python
class EmbeddingService:
    def __init__(self):
        self.tokenizer = AutoTokenizer.from_pretrained("truro7/vn-law-embedding")
        self.model = ORTModelForFeatureExtraction.from_pretrained(
            "truro7/vn-law-embedding", export=True
        )
        self.qdrant_client = QdrantClient(host="localhost", port=6333)

    def encode_text(self, text: str) -> List[float]:
        """Generate 768-dimensional embedding"""
        encoded_input = self.tokenizer(text, padding=True, truncation=True,
                                        return_tensors='pt', max_length=512)
        model_output = self.model(**encoded_input)
        sentence_embeddings = self.mean_pooling(model_output,
                                                  encoded_input['attention_mask'])
        sentence_embeddings = torch.nn.functional.normalize(
            sentence_embeddings, p=2, dim=1
        )
        return sentence_embeddings[0].tolist()

    def vectorize_batch(self, items: list, collection_name: str):
        """Vectorize multiple texts and store in Qdrant"""
        points = []
        for item in items:
            embedding = self.encode_text(item.text)
            point_id = str(uuid.uuid4())

            points.append(PointStruct(
                id=point_id,
                vector=embedding,
                payload={"text": item.text, **item.metadata}
            ))

        self.qdrant_client.upsert(collection_name=collection_name, points=points)
        return len(points)
```

### Performance

- **Embedding Generation**: ~100-300ms per text (depends on length)
- **Batch Size**: Recommended 10-50 texts per batch
- **Model Size**: ~400MB (ONNX optimized)
- **Max Token Length**: 512 tokens (truncated if longer)

---

# 4. Complete Data Flow Examples

## 4.1 End-to-End: User Uploads Document

**Scenario**: User (tenant_id=2) uploads a company regulation document

```
Step 1: Upload Document
──────────────────────────────────────────────────────────────
User → API Gateway → DocumentService
    POST /web-api/document/upload
    Headers: Authorization: Bearer <JWT with tenant_id=2>
    Body: multipart/form-data (file: company-rules.docx)

DocumentService:
    1. CurrentUserProvider.TenantId = 2 (from JWT)
    2. Save to StorageService → file path: "uploads/company-rules.docx"
    3. Create PromptDocument entity:
       - FileName: "company-rules.docx"
       - FilePath: "uploads/company-rules.docx"
       - Action: Upload
       - TenantId: 2 (auto-assigned by UpdateTenancyInterceptor)
    4. Return: { "id": 456, "status": "Upload" }

Step 2: Vectorize Document
──────────────────────────────────────────────────────────────
User → API Gateway → DocumentService
    POST /web-api/document/vectorize/456

DocumentService:
    1. Update Action: Vectorize_Start
    2. Download file from StorageService
    3. Extract hierarchical chunks:
       - Chunk 1: "Chương 1: Quy định chung\nĐiều 1. Định nghĩa..."
       - Chunk 2: "Chương 1: Quy định chung\nĐiều 2. Phạm vi..."
       - ... (total 35 chunks)
    4. Create batches: [chunks 1-10], [chunks 11-20], [chunks 21-30], [chunks 31-35]
    5. Enqueue 4 Hangfire jobs
    6. Update Action: Vectorize_Success
    7. Return: { "status": "Vectorize_Success", "chunks": 35 }

Step 3: Background Vectorization (Hangfire Job 1)
──────────────────────────────────────────────────────────────
VectorizeBackgroundJob.ProcessBatch(chunks[1-10], tenantId=2)
    ↓ POST /vectorize-batch to EmbeddingService
    Body: {
        items: [
            {
                text: "Chương 1: Quy định chung\nĐiều 1. Định nghĩa...",
                metadata: {
                    tenant_id: 2,
                    source_id: 456,
                    file_name: "company-rules.docx",
                    heading1: "Chương 1: Quy định chung",
                    heading2: null,
                    content: "Điều 1. Định nghĩa...",
                    type: 1
                }
            },
            ... (10 items)
        ]
    }

EmbeddingService (Python):
    1. For each item:
       a) Generate 768-dim embedding: [0.123, -0.456, ...]
       b) Create Qdrant point with payload
    2. Upsert to Qdrant collection "vn_law_documents"
    3. Return: { "success": true, "count": 10 }

Step 4: Vectors Stored in Qdrant
──────────────────────────────────────────────────────────────
Qdrant Database:
    Collection: vn_law_documents
    New Points: 35 vectors (from 4 batch jobs)

    Example Point:
    {
        id: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        vector: [0.123, -0.456, 0.789, ...],  // 768 dimensions
        payload: {
            text: "Chương 1: Quy định chung\nĐiều 1. Định nghĩa...",
            tenant_id: 2,  ← Enables tenant filtering
            source_id: 456,
            file_name: "company-rules.docx",
            heading1: "Chương 1: Quy định chung",
            type: 1
        }
    }

RESULT: Document is now searchable for tenant_id=2 users
```

---

## 4.2 End-to-End: User Asks Question with RAG

**Scenario**: User (tenant_id=2) asks "What are our overtime rules?"

```
Step 1: User Sends Message
──────────────────────────────────────────────────────────────
User (Browser) → SignalR WebSocket → ChatService
    Hub.SendMessage(conversationId=1, message="What are our overtime rules?")
    Headers: Authorization: Bearer <JWT with tenant_id=2>

ChatService:
    1. CurrentUserProvider.TenantId = 2
    2. Create ChatMessage:
       - ConversationId: 1
       - Message: "What are our overtime rules?"
       - Role: User
       - TenantId: 2 (auto-assigned)
    3. Broadcast to SignalR: Clients.Group("conversation-1").SendAsync("ReceiveMessage", ...)
    4. Publish RabbitMQ: UserPromptReceivedEvent {
         conversation_id: 1,
         message: "What are our overtime rules?",
         user_id: 123,
         tenant_id: 2
       }

Step 2: ChatProcessor Receives Message
──────────────────────────────────────────────────────────────
RabbitMQ → ChatProcessor (Python)
    Consumer receives UserPromptReceivedEvent
    ↓ Calls ChatBusiness.process_chat_message(...)

Step 3: Generate Query Embedding
──────────────────────────────────────────────────────────────
ChatProcessor → EmbeddingService
    POST /embed
    Body: { "text": "What are our overtime rules?" }

EmbeddingService:
    1. Tokenize: ["what", "are", "our", "overtime", "rules"]
    2. Generate embedding: [0.234, -0.567, 0.890, ...]  // 768-dim
    3. Return: { "vector": [...] }

Step 4: Dual-Query RAG Retrieval
──────────────────────────────────────────────────────────────
ChatProcessor → Qdrant (Query A: State Law)
    Search:
        collection: "vn_law_documents"
        query_vector: [0.234, -0.567, ...]
        filter: { must: [{ key: "tenant_id", match: { value: 1 } }] }
        limit: 1

    Result A (tenant_id=1):
    {
        score: 0.92,
        payload: {
            text: "Điều 98. Làm thêm giờ\n1. Ngày thường: 150%\n2. Ngày nghỉ: 200%\n3. Ngày lễ: 300%",
            tenant_id: 1,
            source_id: 100,
            heading1: "Chương V: Thời giờ làm việc"
        }
    }

ChatProcessor → Qdrant (Query B: Company Rules)
    Search:
        collection: "vn_law_documents"
        query_vector: [0.234, -0.567, ...]
        filter: { must: [{ key: "tenant_id", match: { value: 2 } }] }
        limit: 1

    Result B (tenant_id=2):
    {
        score: 0.88,
        payload: {
            text: "Quy định tăng ca\n1. Ngày thường: 160%\n2. Cuối tuần: 220%\n3. Lễ tết: 350%",
            tenant_id: 2,
            source_id: 456,
            heading1: "Chương 1: Quy định chung"
        }
    }

Step 5: Build Enhanced Prompt
──────────────────────────────────────────────────────────────
ChatProcessor builds context:

    enhanced_prompt = """
    Context information:
    [STATE LAW]
    Điều 98. Làm thêm giờ
    1. Ngày thường: 150%
    2. Ngày nghỉ: 200%
    3. Ngày lễ: 300%

    [COMPANY REGULATION]
    Quy định tăng ca
    1. Ngày thường: 160%
    2. Cuối tuần: 220%
    3. Lễ tết: 350%

    User question: What are our overtime rules?

    Please answer based on the context provided above.
    If both STATE LAW and COMPANY REGULATION are provided,
    compare and contrast them in your response.
    """

Step 6: LLM Generation
──────────────────────────────────────────────────────────────
ChatProcessor → Ollama
    POST /api/chat
    Body: {
        model: "ontocord/vistral:latest",
        messages: [{ role: "user", content: enhanced_prompt }],
        stream: false
    }

Ollama:
    1. Load model: ontocord/vistral:latest (7B Vietnamese LLM)
    2. Generate response: ~15 seconds
    3. Return: {
         message: {
             content: "Về quy định tăng ca:\n\n**Theo luật lao động:**..."
         }
       }

Step 7: Publish Response
──────────────────────────────────────────────────────────────
ChatProcessor → RabbitMQ
    Publish BotResponseCreatedEvent {
        conversation_id: 1,
        message: "Về quy định tăng ca:\n\n**Theo luật lao động:**...",
        user_id: 0,
        model_used: "ontocord/vistral:latest",
        rag_documents_used: 2,
        source_ids: [100, 456]
    }

Step 8: ChatService Receives Response
──────────────────────────────────────────────────────────────
RabbitMQ → ChatService
    Consumer receives BotResponseCreatedEvent

ChatService:
    1. Create ChatMessage:
       - ConversationId: 1
       - Message: "Về quy định tăng ca:..."
       - Role: Assistant
       - ModelUsed: "ontocord/vistral:latest"
       - TenantId: 2 (inherited from conversation)
    2. Broadcast to SignalR: Clients.Group("conversation-1").SendAsync("ReceiveMessage", ...)

Step 9: User Sees Response
──────────────────────────────────────────────────────────────
Browser receives SignalR message:
    Display in chat UI:

    Bot: "Về quy định tăng ca:

    **Theo luật lao động (Điều 98):**
    - Ngày thường: tối thiểu 150% lương
    - Ngày nghỉ: tối thiểu 200% lương
    - Ngày lễ: tối thiểu 300% lương

    **Quy định của công ty:**
    - Ngày thường: 160% (cao hơn mức tối thiểu)
    - Cuối tuần: 220% (cao hơn mức tối thiểu)
    - Lễ tết: 350% (cao hơn mức tối thiểu)

    **Kết luận:** Công ty đang áp dụng mức cao hơn quy định
    pháp luật, đảm bảo tuân thủ và có lợi cho người lao động."

    [Sources: doc-100, doc-456]

RESULT: User receives AI answer with clear comparison between
        state law and company rules, with source attribution
```

---

## 4.3 Cross-Tenant Data Isolation Verification

**Scenario**: Verify tenant_id=2 cannot access tenant_id=3 data

### SQL Database Test

```sql
-- User from tenant_id=2 tries to query all documents
-- (In actual code, TenantId filter is applied in LINQ queries)

SELECT * FROM PromptDocuments WHERE TenantId = 2;
-- Returns: 5 documents (company's own documents)

SELECT * FROM PromptDocuments WHERE TenantId = 3;
-- Returns: 0 documents (access prevented by application code)
```

### Vector Database Test

```python
# User from tenant_id=2 searches for "overtime rules"
query_vector = embed("overtime rules")

# Query with tenant_id=2 filter
results = qdrant_client.search(
    collection_name="vn_law_documents",
    query_vector=query_vector,
    query_filter=Filter(
        should=[
            FieldCondition(key="tenant_id", match=MatchValue(value=1)),  # State law
            FieldCondition(key="tenant_id", match=MatchValue(value=2))   # Own company
        ]
    ),
    limit=5
)

# Results contain:
# - 2 documents from tenant_id=1 (state law - shared)
# - 3 documents from tenant_id=2 (company-specific)
# - 0 documents from tenant_id=3 (isolated)
```

**Verification**:
- ✅ tenant_id=2 can access tenant_id=1 (shared legal base)
- ✅ tenant_id=2 can access tenant_id=2 (own data)
- ✅ tenant_id=2 CANNOT access tenant_id=3 (other company)
- ✅ tenant_id=3 cannot see tenant_id=2's data in any query

---

## Appendix A: Configuration Reference

### JWT Configuration

```csharp
// Infrastructure/Authentication/AuthorizationConstants.cs
public static class AuthorizationConstants
{
    public const string JWT_SECRET_KEY = "YourSecretKeyHere...";
    public const string TOKEN_CLAIMS_TENANT = "tenant_id";
    public const string TOKEN_CLAIMS_TYPE_SCOPE = "scope";
    public const string POLICY_ADMIN = "IsAdmin";
}
```

### Database Connection Strings

```json
{
  "ConnectionStrings": {
    "AccountDbContext": "Server=localhost;Database=AccountDb;Trusted_Connection=true;TrustServerCertificate=true;",
    "TenantDbContext": "Server=localhost;Database=TenantDb;...",
    "DocumentDbContext": "Server=localhost;Database=DocumentDb;...",
    "ChatDbContext": "Server=localhost;Database=ChatDb;..."
  }
}
```

### Python Services Configuration

**EmbeddingService (.env)**:
```bash
MODEL_NAME=truro7/vn-law-embedding
QDRANT_HOST=localhost
QDRANT_PORT=6333
QDRANT_COLLECTION=vn_law_documents
FASTAPI_PORT=8000
```

**ChatProcessor (.env)**:
```bash
OLLAMA_BASE_URL=http://localhost:11434
OLLAMA_MODEL=ontocord/vistral:latest
OLLAMA_TIMEOUT=300
QDRANT_HOST=localhost
QDRANT_PORT=6333
QDRANT_COLLECTION=vn_law_documents
EMBEDDING_SERVICE_URL=http://localhost:8000
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
FASTAPI_PORT=8001
```

---

## Appendix B: Performance Metrics

| Operation | Avg Time | Notes |
|-----------|----------|-------|
| JWT Generation | 5-10ms | TokenClaimsService |
| Document Upload | 100-500ms | Depends on file size |
| Hierarchical Chunking | 200-800ms | Depends on document size |
| Embedding Generation | 100-300ms | Per chunk (768-dim) |
| Vector Upsert | 10-50ms | Batch of 10 chunks |
| Vector Search | 10-50ms | Qdrant query |
| Dual-Query RAG | 20-100ms | Two parallel searches |
| LLM Generation | 5-60s | Depends on response length |
| End-to-End Chat | 6-62s | Including all steps |

---

## Appendix C: Key Takeaways

### Multitenancy Strategy Summary

1. **Automatic Tenant Assignment**: EF Core interceptor eliminates manual TenantId assignment
2. **Dual-Query RAG**: Guarantees cross-referencing between state law and company rules
3. **JWT-Based Propagation**: Tenant context flows seamlessly through all layers
4. **Shared Legal Base**: tenant_id=1 provides common legal foundation for all tenants
5. **No Global Query Filters**: Explicit filtering in application code (except soft-delete)
6. **Vector Metadata Isolation**: tenant_id stored in Qdrant payload for filter-based isolation

### Architecture Highlights

1. **Microservices**: Each service has isolated database and clear responsibilities
2. **API Gateway**: YARP provides unified entry point with Swagger aggregation
3. **Async Processing**: Hangfire and RabbitMQ enable background/async operations
4. **Real-Time Communication**: SignalR for WebSocket-based chat
5. **Python AI Services**: Specialized services for embeddings and RAG processing
6. **ONNX Optimization**: Fast inference for Vietnamese legal text embeddings

---

**Document End**

**Version**: 2.0.0
**Last Updated**: 2025-12-17
**Authors**: AI Chat Development Team
**Primary Focus**: Multitenancy Data Management Strategy
