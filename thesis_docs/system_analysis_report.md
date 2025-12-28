# AIChat2025 - System Analysis Report
## Comprehensive Technical Documentation for Graduation Thesis

**Generated:** 2025-12-28
**Project:** Multi-tenant RAG Legal Chat System
**Technology Stack:** .NET 9.0, Python (FastAPI), Qdrant, RabbitMQ, Ollama

---

## EXECUTIVE SUMMARY

AIChat2025 is an **enterprise-grade multi-tenant RAG (Retrieval-Augmented Generation) system** for Vietnamese legal document consultation. The system combines modern microservices architecture, AI-powered chat, and real-time communication to deliver accurate legal guidance based on both statutory law and company-specific regulations.

**Key Metrics:**
- **9 Microservices** (5 .NET, 2 Python, 1 Gateway, 1 Web App)
- **144 C# Files** across backend services
- **27 Python Files** for AI workers
- **11 Razor Views** for frontend
- **Multi-tenant Architecture** with row-level security
- **Dual-RAG Implementation** (Company Rules + Legal Base)
- **Real-time Communication** via SignalR WebSockets

---

## 1. MICROSERVICES ARCHITECTURE

### 1.1 Service Inventory

| Service | Technology | Port | Purpose | Database |
|---------|------------|------|---------|----------|
| **AccountService** | .NET 9 | 5050 | Authentication, User Management | SQL Server |
| **TenantService** | .NET 9 | 5062 | Tenant Management, Organization Setup | SQL Server |
| **DocumentService** | .NET 9 | 5165 | Document Storage, Background Vectorization | SQL Server |
| **StorageService** | .NET 9 | 5113 | File Storage via MinIO (S3-compatible) | N/A |
| **ChatService** | .NET 9 | 5218 | Chat History, SignalR Hub, RabbitMQ Producer | SQL Server |
| **EmbeddingService** | Python/FastAPI | 8000 | Document Embedding, Qdrant Integration | Qdrant |
| **ChatProcessor** | Python/FastAPI | 8001 | RAG Pipeline, LLM Integration, RabbitMQ Consumer | Qdrant |
| **ApiGateway** | .NET 9 YARP | 5000 | Reverse Proxy, Unified Swagger, Auth | N/A |
| **WebApp** | ASP.NET MVC | N/A | Frontend UI, Cookie Auth | N/A |

### 1.2 Architecture Diagram (Textual Representation)

```
┌─────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                          │
│                      (Web Browser)                           │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                       WebApp (MVC)                           │
│            Cookie Auth, SignalR Client, jQuery               │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                  ApiGateway (YARP 2.3.0)                     │
│          JWT Validation, Route Aggregation                   │
└───┬───────┬──────┬──────┬──────┬────────────────────────────┘
    │       │      │      │      │
    ▼       ▼      ▼      ▼      ▼
 Account  Tenant  Doc   Storage  Chat
 Service  Service Service Service Service
   │        │      │       │       │
   └────────┴──────┴───────┴───────┘
                   │
                   ▼
          ┌──────────────────┐
          │   SQL Server      │
          │   (Shared DB)     │
          └──────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    MESSAGE QUEUE LAYER                       │
│                      (RabbitMQ 3)                            │
└───┬───────────────────────────────────────────────────┬─────┘
    │                                                     │
    │ [UserPromptReceived]                   [BotResponseCreated]
    │                                                     │
    ▼                                                     ▼
┌────────────────────┐                        ┌──────────────────┐
│  ChatService       │                        │  ChatService     │
│  (Publisher)       │                        │  (Consumer)      │
└────────────────────┘                        └──────────────────┘
    │
    │ Publishes event
    ▼
┌─────────────────────────────────────────────────────────────┐
│               ChatProcessor (Python)                         │
│        RAG Pipeline, Ollama Integration                      │
└───┬────────────────────────────────────────────────────┬────┘
    │                                                     │
    ▼                                                     ▼
┌────────────────────┐                        ┌──────────────────┐
│  EmbeddingService  │                        │   Ollama LLM     │
│   (Qdrant API)     │                        │  (vistral model) │
└────────────────────┘                        └──────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│                Qdrant Vector Database                        │
│       Multi-tenant Collections, Cosine Similarity            │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│               INFRASTRUCTURE LAYER                           │
│  SQL Server | RabbitMQ | Qdrant | Ollama | MinIO            │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. MULTI-TENANT ARCHITECTURE

### 2.1 Implementation Strategy

**Pattern:** Row-Level Security (Shared Database with Tenant Filtering)

### 2.2 Core Components

**2.2.1 Base Entity Hierarchy**

```
BaseEntity (Id: int)
    ↓
AuditableEntity (+CreatedAt, +LastModifiedAt, +IsDeleted, +CreatedBy)
    ↓
TenancyEntity (+TenantId: int)
```

**File:** `D:\Project\AIChat2025\Infrastructure\Entities\BaseEntity.cs`

**2.2.2 Tenant Context Provider**

**File:** `D:\Project\AIChat2025\Infrastructure\Tenancy\CurrentTenantProvider.cs`

**Modes:**
1. **HTTP Context Mode** (Web API):
   - Extracts `tenant_id` from JWT claims
   - Automatically applied to all authenticated requests

2. **Manual Impersonation Mode** (Background Workers):
   - RabbitMQ consumers decode JWT from message payload
   - Calls `SetTenantId(tenantId)` before database operations

**2.2.3 Data Isolation Mechanisms**

**Write Operations - UpdateTenancyInterceptor:**
```csharp
public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
{
    foreach (var entry in dbContext.ChangeTracker.Entries<TenancyEntity>())
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.TenantId = currentTenantProvider.TenantId ?? throw new InvalidOperationException();
        }
    }
}
```

**Read Operations - TenancySpecification:**
```csharp
public class TenancySpecification<T> : Specification<T> where T : TenancyEntity
{
    public TenancySpecification(int? tenantId)
    {
        // Super Admin (TenantId = 1) bypasses filter
        if (tenantId == null || tenantId == 1)
            return;

        Query.Where(x => x.TenantId == tenantId);
    }
}
```

### 2.3 Data Flow Diagrams

**Authentication Flow:**
```
User Login
    ↓
WebApp → ApiGateway → AccountService
    ↓
JWT Generated (Claims: UserId, TenantId, IsAdmin, Scope)
    ↓
Cookie Stored (7-day expiration, HttpOnly)
    ↓
Subsequent Requests Include JWT in Header
    ↓
CurrentTenantProvider Extracts TenantId from Claims
    ↓
Database Queries Automatically Filtered by TenantId
```

**Background Processing Flow:**
```
ChatService Publishes RabbitMQ Event (includes JWT token)
    ↓
ChatProcessor Consumer Receives Message
    ↓
TokenDecoder Extracts TenantId from JWT
    ↓
CurrentTenantProvider.SetTenantId(tenantId) [Manual]
    ↓
RAG Pipeline Executes with Tenant Context
    ↓
All Database Queries Filtered by TenantId
```

---

## 3. COMMUNICATION PATTERNS

### 3.1 RabbitMQ Event-Driven Architecture

**Queue Topology:**

```
ChatService (Producer)
    ↓
[UserPromptReceived Exchange - Fanout]
    ↓
[UserPromptReceived Queue]
    ↓
ChatProcessor (Consumer)
    ↓
RAG Processing
    ↓
[BotResponseCreated Exchange - Fanout]
    ↓
[BotResponseCreated Queue]
    ↓
ChatService.BotResponseConsumer
    ↓
SignalR Hub Broadcast
```

**Event Schemas:**

**UserPromptReceivedEvent:**
```csharp
{
    ConversationId: int,
    Message: string,
    Token: string (JWT),
    Timestamp: DateTime,
    SystemInstruction: PromptConfig[] // Keyword mappings
}
```

**BotResponseCreatedEvent:**
```csharp
{
    ConversationId: int,
    Message: string,
    Token: string (JWT),
    Timestamp: DateTime,
    ModelUsed: string (e.g., "ontocord/vistral:latest")
}
```

### 3.2 SignalR Real-Time Communication

**Hub Implementation:** `D:\Project\AIChat2025\Services\ChatService\Hubs\ChatHub.cs`

**Hub Methods:**
- `SendMessage(conversationId, message, userId)` - User sends message
- `JoinConversation(conversationId)` - Subscribe to updates
- `LeaveConversation(conversationId)` - Unsubscribe

**Client Events:**
- `ReceiveMessage` - User message broadcast
- `BotResponse` - AI response broadcast

**Connection Management:**
- WebSocket primary transport
- Fallback: Server-Sent Events → Long Polling
- Automatic reconnection with exponential backoff [0s, 2s, 5s, 10s, 30s]
- Group-based messaging (`conversation-{id}`)

### 3.3 HTTP REST (Synchronous)

**API Gateway Routing:**
- `/web-api/account/**` → AccountService
- `/web-api/tenant/**` → TenantService
- `/web-api/document/**` → DocumentService
- `/web-api/storage/**` → StorageService
- `/web-api/chat/**` → ChatService
- `/hubs/chat/**` → ChatService (SignalR)

---

## 4. DOCUMENT PROCESSING PIPELINE

### 4.1 Hierarchical Semantic Chunking

**File:** `D:\Project\AIChat2025\Services\DocumentService\Features\PromptDocumentBusiness.cs`

**Pipeline Stages:**

**Stage 1: Document Upload**
```
User uploads .docx file
    ↓
PromptDocumentBusiness.CreateDocument()
    ↓
Standardize headings using regex:
  - RegexHeading1: Chương/Chapter (e.g., "CHƯƠNG I")
  - RegexHeading2: Mục/Section (e.g., "Mục 1")
  - RegexHeading3: Điều/Article (e.g., "Điều 5")
    ↓
Upload to MinIO (S3-compatible storage)
    ↓
Set DocumentAction: Upload → Standardization → Upload
```

**Stage 2: Hierarchical Chunking**
```
PromptDocumentBusiness.VectorizeDocument()
    ↓
Download document from MinIO
    ↓
ExtractHierarchicalChunks():
  For each Article (Điều):
    Chunk = {
      Heading1: "Chương XV",
      Heading2: "Mục 3",
      Heading3: "Điều 212",
      Content: "<article content>",
      FullText: Heading1 + Heading2 + Content
    }
    ↓
Enqueue Hangfire Background Job
  Batch Size: 10 chunks per job
```

**Stage 3: Background Vectorization**
```
VectorizeBackgroundJob.Execute()
    ↓
Build Metadata Payload:
  {
    source_id: documentId,
    file_name: "labor_law.docx",
    document_name: "Bộ luật Lao động 2019 (Law No. 45/2019/QH14)",
    father_doc_name: null (or parent law for decrees),
    heading1: "Chương XV",
    heading2: "Mục 3",
    content: "<content text>",
    tenant_id: 1 (legal base) or X (company rules),
    type: 1 (Law) or 2 (Decree)
  }
    ↓
POST /vectorize-batch to EmbeddingService
```

**Stage 4: Embedding & Vector Storage**
```
EmbeddingService.vectorize_batch()
    ↓
For each chunk:
  tokenize(text, max_length=512)
  embedding = model(tokens) // truro7/vn-law-embedding
  mean_pooling(embedding)
  L2_normalize(embedding)  // 768-dimensional vector
    ↓
Store in Qdrant:
  PointStruct(
    id: UUID,
    vector: [float; 768],
    payload: metadata
  )
    ↓
Collection: vn_law_documents
Distance Metric: COSINE
```

### 4.2 Document Metadata Schema

**Qdrant Payload Example:**
```json
{
  "text": "Điều 212. Thu hồi Giấy chứng nhận đăng ký địa điểm kinh doanh...",
  "source_id": 42,
  "file_name": "bo-luat-lao-dong-2019.docx",
  "document_name": "Bộ luật Lao động 2019 (Law No. 45/2019/QH14)",
  "father_doc_name": "",
  "heading1": "Chương XV: Các hành vi vi phạm",
  "heading2": "Mục 3: Xử lý đối với chi nhánh",
  "content": "Chi nhánh sẽ bị thu hồi Giấy chứng nhận...",
  "tenant_id": 1,
  "type": 1
}
```

---

## 5. RAG (RETRIEVAL-AUGMENTED GENERATION) PIPELINE

### 5.1 Complete RAG Flow

**File:** `D:\Project\AIChat2025\Services\ChatProcessor\src\business.py`

**9-Step Pipeline:**

**STEP 1: Query Expansion**
```python
def _expand_query_with_prompt_config(message, prompt_config):
    # Replace abbreviations with full descriptions
    # Example: "OT" → "Overtime Payment"
    # Goal: Enhance semantic retrieval accuracy
```

**STEP 2: Parallel Embedding & Retrieval**
```python
query_embedding = await qdrant_service.get_embedding(enhanced_message)

# Dual retrieval: Legal Base (tenant_id=1) + Company Rules (tenant_id=X)
(legal_base_results, company_rule_results) = await asyncio.gather(
    qdrant_service.search_exact_tenant(query_embedding, tenant_id=1, limit=1),
    qdrant_service.search_exact_tenant(query_embedding, tenant_id=user_tenant, limit=1)
)
```

**Filters:**
- Similarity Threshold: 0.7
- Tenant Filtering: `tenant_id IN (1, user_tenant)`

**STEP 3: Scenario Detection**
```python
scenario = _detect_scenario(company_results, legal_results)
# Returns: "BOTH" | "COMPANY_ONLY" | "LEGAL_ONLY" | "NONE"
```

**STEP 4: Context Structuring**
```
═══════════════════════════════════════
**═══ NỘI QUY CÔNG TY ═══**
(Quy định nội bộ - ưu tiên áp dụng)
═══════════════════════════════════════

[Nội quy: Quy chế Lương thưởng 2024 - Chương III - Điều 15]
<content text>

═══════════════════════════════════════
**═══ VĂN BẢN PHÁP LUẬT ═══**
(Quy định của Nhà nước - làm cơ sở đối chiếu)
═══════════════════════════════════════

[Bộ luật Lao động 2019 - Chương XV - Điều 212]
<content text>
```

**STEP 5: System Prompt Selection**

**Comparison Mode (BOTH scenario):**
```
⛔ PROHIBITIONS:
- NO step-by-step reasoning
- NO instruction text in output
- NO prefixes like "Trả lời:", "Câu trả lời:"

✓ OUTPUT FORMAT:
Theo [Company Doc], công ty quy định [value], [valid/higher/lower] than [Legal Doc] minimum of [value].
```

**Single Source Mode:**
```
OUTPUT: Theo [Document - Article X], [content].
```

**STEP 6: Terminology Injection**
```python
# Inject PromptConfig definitions into system prompt
# Ensures LLM understands abbreviations
```

**STEP 7: LLM Generation (Ollama)**
```python
response = await ollama_service.generate_response(
    prompt=enhanced_prompt,
    conversation_history=[
        {'role': 'system', 'content': system_prompt},
        {'role': 'system', 'content': terminology_definitions}
    ],
    temperature=0.1  # Low temperature for factual responses
)
```

**Model:** `ontocord/vistral:latest` (Vietnamese-finetuned LLM)

**STEP 8: Post-Processing Cleanup**
```python
def _cleanup_response(response):
    # Multi-pass removal of instruction prefixes
    # Remove reasoning steps (Bước 1, Bước 2, etc.)
    # Remove instruction sentences
```

**STEP 9: Async Evaluation Logging**
```python
# Fire-and-forget RAGAS evaluation
asyncio.create_task(
    evaluation_logger.log_interaction_async(
        question=message,
        contexts=contexts,
        answer=response,
        ...
    )
)
```

### 5.2 Citation Generation Logic

**Problem Solved:** Original implementation generated placeholders like "Document #1" instead of actual document names.

**Solution Implemented:**

**Helper Method:**
```python
def _build_citation_label(result, is_company_rule, index):
    payload = result.payload
    document_name = payload.get('document_name', '')
    heading1 = payload.get('heading1', '')
    heading2 = payload.get('heading2', '')

    # Build hierarchical label
    label_parts = [document_name]
    if heading1 and heading2:
        label_parts.append(f"{heading1} - {heading2}")

    label = " - ".join(label_parts)

    if is_company_rule and document_name:
        return f"[Nội quy: {label}]"
    else:
        return f"[{label}]"
```

**Output Example:**
```
[Bộ luật Lao động 2019 (Law No. 45/2019/QH14) - Chương XV - Điều 212]
According to Article 212 of Labor Code 2019, a branch will have its certificate revoked if...
```

---

## 6. DATABASE SCHEMA & DATA MODELS

### 6.1 Entity Relationship Diagram (Textual)

```
┌──────────────────────┐
│      Tenant          │
│  ─────────────────   │
│  Id (PK)             │
│  Name                │
│  Description         │
│  IsActive            │
│  Permissions (JSON)  │
└─────────┬────────────┘
          │ 1
          │
          │ N
┌─────────┴────────────┐     ┌─────────────────────────────┐
│      Account         │     │    ChatConversation         │
│  ─────────────────   │     │  ────────────────────────   │
│  Id (PK)             │     │  Id (PK)                    │
│  TenantId (FK)       │────>│  TenantId (FK)              │
│  Email               │     │  UserId (not FK)            │
│  Password (hashed)   │     │  Title                      │
│  Name                │     │  LastMessageAt              │
│  IsAdmin             │     └────────┬────────────────────┘
│  IsActive            │              │ 1
│  TenancyActive       │              │
└──────────────────────┘              │ N
                                      │
                         ┌────────────┴──────────────┐
                         │    ChatMessage            │
                         │  ──────────────────────   │
                         │  Id (PK)                  │
                         │  TenantId (FK)            │
                         │  ConversationId (FK)      │
                         │  Message                  │
                         │  UserId (not FK)          │
                         │  Timestamp                │
                         │  Type (User/Bot enum)     │
                         └───────────────────────────┘

┌──────────────────────┐     ┌─────────────────────────────┐
│  PromptDocument      │     │     PromptConfig            │
│  ─────────────────   │     │  ────────────────────────   │
│  Id (PK)             │     │  Id (PK)                    │
│  TenantId (FK)       │     │  TenantId (FK)              │
│  FileName            │     │  Key                        │
│  FilePath (MinIO)    │     │  Value (description)        │
│  DocumentName        │     └─────────────────────────────┘
│  Action (enum)       │
│  DocumentType (enum) │
│  FatherDocumentId    │
│  IsApproved          │
└──────────────────────┘
```

### 6.2 Key Indexes

**ChatConversations:**
- `IX_ChatConversations_LastMessageAt`
- `IX_ChatConversations_TenantId_UserId`

**ChatMessages:**
- `IX_ChatMessages_ConversationId`
- `IX_ChatMessages_TenantId_UserId`

**PromptConfigs:**
- `IX_PromptConfigs_Key`
- `IX_PromptConfigs_TenantId`

### 6.3 Database Migrations History

**AccountService:**
- 20251117154134_InitDb
- 20251210145338_InitDbAccount
- 20251216174314_InitDbTenancy
- 20251216175403_DefaultAccount (seeding)
- 20251223180646_AddPermissions

**ChatService:**
- 20251216113605_InitDb
- 20251217100257_AddChatType
- 20251218114427_addPromptConfig

**DocumentService:**
- 20251210145716_InitDbDocument
- 20251226103457_AddDocType
- 20251226103933_AddDocName

---

## 7. SECURITY & AUTHENTICATION

### 7.1 JWT Implementation

**File:** `D:\Project\AIChat2025\Infrastructure\Authentication\TokenClaimsService.cs`

**Configuration:**
- **Algorithm:** HMAC-SHA256
- **Secret Key:** `45dfghdfgh2345kfhdfgh2fg34534523sdfgse45` (hardcoded - security issue)
- **Expiration:** 7 days
- **Issuer/Audience:** Not configured (should be added)

**Claims Structure:**
```csharp
{
  ClaimTypes.NameIdentifier: userId,
  ClaimTypes.Name: username,
  "tenant_id": tenantId,
  "is_admin": "True"/"False",
  "scope": "scope_web" / "scope_mobile",
  "user_id": userId
}
```

**Authorization Policies:**
- `scope_web` - Web application access
- `scope_mobile` - Mobile application access (future)
- Admin policy enforcement via `is_admin` claim

### 7.2 Password Security

**BCrypt Hashing:**
```csharp
// Registration
hashedPassword = PasswordHasher.HashPassword(plainPassword);

// Login
bool isValid = PasswordHasher.VerifyPassword(plainPassword, hashedPassword);
```

### 7.3 Security Strengths & Weaknesses

**✅ Strengths:**
- BCrypt password hashing
- Multi-tenant data isolation
- JWT authentication
- Soft delete for audit trails
- Cookie HttpOnly flag (XSS protection)

**⚠️ Weaknesses:**
- **Critical:** Hardcoded JWT secret key in source code
- No refresh token rotation mechanism
- SQL Server password exposed in docker-compose.yml
- No rate limiting on API endpoints
- CORS AllowAnyOrigin policy (too permissive)
- No HTTPS enforcement
- JWT expiration too long (7 days)
- No JWT revocation mechanism

---

## 8. INFRASTRUCTURE & DEPLOYMENT

### 8.1 Docker Compose Architecture

**File:** `D:\Project\AIChat2025\docker-compose.yml`

**Infrastructure Services:**

| Service | Image | Port(s) | Volume | Purpose |
|---------|-------|---------|--------|---------|
| **sqlserver** | mcr.microsoft.com/mssql/server:2022-latest | 1433 | N/A | Shared SQL database |
| **rabbitmq** | rabbitmq:3-management | 5672, 15672 | N/A | Message queue |
| **qdrant** | qdrant/qdrant:latest | 6333 | G:/Mount/qdrant | Vector database |
| **ollama** | ollama/ollama:latest | 11434 | G:/Mount/ollama | LLM inference |
| **minio** | minio/minio:latest | 9000, 9001 | G:/Mount/minio | Object storage |

**Application Services:**

| Service | Port Mapping | Depends On |
|---------|--------------|------------|
| accountservice | 5050:8080 | sqlserver, rabbitmq |
| tenantservice | 5062:8080 | sqlserver, rabbitmq |
| documentservice | 5165:8080 | sqlserver, rabbitmq |
| storageservice | 5113:8080 | minio |
| chatservice | 5218:8080 | sqlserver, rabbitmq |
| embeddingservice | 8000:8000 | qdrant |
| chatprocessor | 8001:8001 | rabbitmq, qdrant, ollama |
| apigateway | 5000:8080 | All services |

**Network:** aichat-network (bridge)

### 8.2 Service Configuration

**Environment Variables Pattern:**
- **SQL Server:** `Server=sqlserver;Database=AIChat2025;User Id=sa;Password=YourStrong!Passw0rd`
- **RabbitMQ:** `localhost:5672` (guest/guest)
- **Qdrant:** `localhost:6333`
- **Ollama:** `http://localhost:11434`
- **MinIO:** `localhost:9000` (admin/password)

---

## 9. TECHNOLOGY STACK SUMMARY

### 9.1 Backend (.NET 9)

**Frameworks:**
- ASP.NET Core 9.0
- Entity Framework Core 9.0
- YARP Reverse Proxy 2.3.0
- Minimal APIs pattern

**Libraries:**
- MassTransit 8.3.4 (RabbitMQ integration)
- Ardalis.Specification 9.3.1 (Repository pattern)
- SignalR 1.1.0 (Real-time communication)
- Hangfire 1.8.17 (Background jobs)
- Serilog 10.0.0 (Logging)
- DocumentFormat.OpenXml 3.3.0 (Word processing)
- Minio 7.0.0 (S3 client)
- Qdrant.Client 1.16.1 (Vector DB client)

### 9.2 AI & Python

**Frameworks:**
- FastAPI 0.115.0
- Uvicorn 0.32.0
- Pydantic 2.9.2

**AI Libraries:**
- Transformers (HuggingFace)
- ONNX Runtime (optimized inference)
- torch (PyTorch backend)
- qdrant-client 1.11.3
- ragas (RAG evaluation)

**Messaging:**
- aio-pika 9.4.3 (Async RabbitMQ)
- httpx 0.27.0 (Async HTTP)
- PyJWT 2.8.0 (JWT parsing)

**Model:**
- **Embedding:** truro7/vn-law-embedding (768-dimensional)
- **LLM:** ontocord/vistral:latest (Vietnamese finetuned)

### 9.3 Frontend

**Framework:** ASP.NET MVC (Razor views)

**Libraries:**
- jQuery 3.x
- Bootstrap 5
- SignalR JavaScript Client 8.0.0
- jQuery Validation

**Styling:** Custom CSS with Google Gemini-inspired design

### 9.4 Infrastructure

- **SQL Server 2022** - Relational database
- **RabbitMQ 3** - Message queue
- **Qdrant latest** - Vector database
- **Ollama latest** - LLM hosting
- **MinIO latest** - Object storage
- **Docker Compose** - Container orchestration

---

## 10. CODE STATISTICS

**File Counts:**
- **C# Files:** 144 (excluding migrations, obj, bin)
- **Python Files:** 27 (excluding __pycache__, venv)
- **Razor Views:** 11
- **JavaScript Files:** 6

**Service Breakdown:**
- **AccountService:** ~15 files
- **TenantService:** ~10 files
- **DocumentService:** ~20 files
- **StorageService:** ~8 files
- **ChatService:** ~25 files
- **Infrastructure:** ~35 files (shared)
- **ApiGateway:** ~5 files
- **WebApp:** ~15 files
- **EmbeddingService:** ~10 Python files
- **ChatProcessor:** ~17 Python files

**Estimated Lines of Code:**
- **Backend (.NET):** ~15,000 - 20,000 LOC
- **AI Workers (Python):** ~3,000 - 4,000 LOC
- **Frontend (Razor + JS):** ~2,000 - 3,000 LOC
- **Total:** ~20,000 - 27,000 LOC

**API Endpoints:** 30+ REST endpoints across 5 microservices

**Database Tables:** 8 main entities (Account, Tenant, Permission, ChatConversation, ChatMessage, PromptConfig, PromptDocument)

---

## 11. KEY DESIGN DECISIONS

### 11.1 Architectural Decisions

**1. Microservices over Monolith**
- **Decision:** Split into 9 independent services
- **Rationale:** Scalability, independent deployment, technology flexibility (.NET + Python)
- **Trade-off:** Increased complexity, distributed transactions

**2. Row-Level Multi-Tenancy over Database per Tenant**
- **Decision:** Shared database with TenantId filtering
- **Rationale:** Cost-effective, easier maintenance, simpler backups
- **Trade-off:** Requires careful query filtering, risk of data leakage

**3. RabbitMQ over Direct HTTP for AI Processing**
- **Decision:** Event-driven async messaging
- **Rationale:** Decouples services, handles spikes, retries on failure
- **Trade-off:** Eventual consistency, debugging complexity

**4. Qdrant over Pinecone/Weaviate**
- **Decision:** Qdrant for vector storage
- **Rationale:** Open-source, self-hosted, excellent filtering, multi-tenancy support
- **Trade-off:** Self-managed infrastructure

**5. Ollama (Local LLM) over OpenAI API**
- **Decision:** Self-hosted Vietnamese LLM
- **Rationale:** Data privacy, cost control, latency, customization
- **Trade-off:** Requires GPU, model maintenance, potentially lower quality

**6. Hierarchical Chunking over Fixed-Size Chunking**
- **Decision:** Article-level semantic chunks
- **Rationale:** Preserves legal document structure, better context
- **Trade-off:** Variable chunk sizes, complex parsing logic

### 11.2 Technology Choices Justification

**Why .NET 9?**
- Cross-platform, high performance, mature ecosystem
- Strong typing, excellent tooling (Visual Studio)
- Built-in dependency injection, minimal APIs

**Why Python for AI Workers?**
- Best ecosystem for AI/ML (HuggingFace, ONNX, PyTorch)
- FastAPI for high-performance async APIs
- Interop with .NET via RabbitMQ/HTTP

**Why SignalR?**
- Native .NET real-time framework
- Automatic fallback transports
- Tight integration with ASP.NET authentication

**Why YARP over Ocelot?**
- Microsoft-maintained, modern design
- Better performance, active development
- Swagger aggregation support

---

## 12. CHALLENGES & SOLUTIONS

### 12.1 Technical Challenges

**Challenge 1: Cross-Platform JWT Validation (.NET ↔ Python)**
- **Problem:** Python services need to validate .NET-generated JWT
- **Solution:** Shared secret key, PyJWT library with same algorithm
- **File:** `D:\Project\AIChat2025\Services\ChatProcessor\app\utils\jwt_validator.py`

**Challenge 2: Multi-Tenant Background Jobs**
- **Problem:** Hangfire jobs lack HTTP context for tenant extraction
- **Solution:** Manual tenant impersonation via `CurrentTenantProvider.SetTenantId()`
- **File:** `D:\Project\AIChat2025\Services\DocumentService\Features\VectorizeBackgroundJob.cs`

**Challenge 3: Real-Time Chat with Async RAG**
- **Problem:** LLM generation takes 10-30 seconds, blocking SignalR connection
- **Solution:** RabbitMQ decouples request/response, SignalR broadcasts completion
- **Flow:** SignalR → ChatService → RabbitMQ → ChatProcessor → RabbitMQ → ChatService → SignalR

**Challenge 4: Vietnamese Text Processing**
- **Problem:** Generic embedding models poor for Vietnamese legal text
- **Solution:** Specialized model `truro7/vn-law-embedding` trained on Vietnamese
- **Optimization:** ONNX Runtime for faster inference

**Challenge 5: Citation Accuracy**
- **Problem:** LLM generated generic "Document #1" instead of real document names
- **Solution:** Extract metadata (document_name, headings) from Qdrant payload, inject into context
- **Implementation:** `_build_citation_label()` helper method

**Challenge 6: Prompt Leakage**
- **Problem:** LLM outputting instruction text ("Trích dẫn chính xác từ ngữ cảnh...")
- **Solution:** Multi-pass cleanup with regex, explicit prohibitions in system prompt
- **File:** `D:\Project\AIChat2025\Services\ChatProcessor\src\business.py` (`_cleanup_response`)

---

## 13. FUTURE IMPROVEMENTS

### 13.1 Security Enhancements
- [ ] Move JWT secret to environment variables / Azure Key Vault
- [ ] Implement refresh token rotation
- [ ] Add rate limiting middleware (AspNetCoreRateLimit)
- [ ] Enable HTTPS enforcement
- [ ] Configure JWT issuer/audience validation
- [ ] Implement JWT token revocation (Redis blacklist)

### 13.2 Performance Optimizations
- [ ] Add Redis caching for frequently accessed documents
- [ ] Implement query result caching in Qdrant
- [ ] Add CDN for static assets
- [ ] Optimize database indexes based on query patterns
- [ ] Implement connection pooling for RabbitMQ

### 13.3 Feature Additions
- [ ] Multi-language support (English, Vietnamese)
- [ ] Document version control and diff
- [ ] Advanced analytics dashboard (conversation insights)
- [ ] Export chat history to PDF/Word
- [ ] Voice input/output integration
- [ ] Mobile application (React Native)

### 13.4 AI Improvements
- [ ] Implement hybrid search (keyword + semantic)
- [ ] Add document summarization feature
- [ ] Implement contract analysis workflows
- [ ] Fine-tune LLM on company-specific legal data
- [ ] Add confidence scores to AI responses
- [ ] Implement multi-turn conversation context

---

## 14. APPENDIX: FILE PATHS REFERENCE

### Critical Files Index

**Backend Services:**
- Account Auth: `D:\Project\AIChat2025\Services\AccountService\Endpoints\AuthEndpoint.cs`
- Chat Business: `D:\Project\AIChat2025\Services\ChatService\Features\ChatBusiness.cs`
- Document Business: `D:\Project\AIChat2025\Services\DocumentService\Features\PromptDocumentBusiness.cs`
- Hangfire Jobs: `D:\Project\AIChat2025\Services\DocumentService\Features\VectorizeBackgroundJob.cs`

**AI Workers:**
- RAG Pipeline: `D:\Project\AIChat2025\Services\ChatProcessor\src\business.py`
- Embedding Service: `D:\Project\AIChat2025\Services\EmbeddingService\src\business.py`
- RabbitMQ Consumer: `D:\Project\AIChat2025\Services\ChatProcessor\app\services\rabbitmq_service.py`

**Infrastructure:**
- JWT Service: `D:\Project\AIChat2025\Infrastructure\Authentication\TokenClaimsService.cs`
- Tenant Provider: `D:\Project\AIChat2025\Infrastructure\Tenancy\CurrentTenantProvider.cs`
- Base DbContext: `D:\Project\AIChat2025\Infrastructure\Database\BaseDbContext.cs`
- Tenancy Interceptor: `D:\Project\AIChat2025\Infrastructure\Database\UpdateTenancyInterceptor.cs`

**Frontend:**
- Chat UI: `D:\Project\AIChat2025\WebApp\Views\Chat\Index.cshtml`
- Chat JavaScript: `D:\Project\AIChat2025\WebApp\wwwroot\Scripts\Chat\Chat.js`
- SignalR Hub: `D:\Project\AIChat2025\Services\ChatService\Hubs\ChatHub.cs`

**Deployment:**
- Docker Compose: `D:\Project\AIChat2025\docker-compose.yml`
- API Gateway Config: `D:\Project\AIChat2025\ApiGateway\Config\appsettings.json`

---

**END OF SYSTEM ANALYSIS REPORT**

---

**Next Steps for Thesis:**
1. Review chapter outlines in `thesis_docs/chapter_outlines/`
2. Create diagrams from `thesis_docs/diagrams_to_create.md`
3. Collect screenshots for Chapter 4
4. Review `thesis_docs/technology_inventory.md` for Chapter 3
5. Check `thesis_docs/missing_implementations.md` for Chapter 6 (Future Work)
