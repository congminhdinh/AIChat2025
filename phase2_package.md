# PHASE 2: Package Analysis for 4.1.2 (UML Package Diagram)

## MỤC TIÊU
Phân tích cấu trúc packages ở **HIGH-LEVEL** để tạo outline cho **1 biểu đồ UML Package Diagram duy nhất** thể hiện TOÀN BỘ hệ thống AIChat2025, với **NHẤN MẠNH** đặc thù Multi-tenant và RAG.

## INPUT REQUIRED
Đọc file: `project_map.md` (đã tạo từ Phase 1)

## BỐI CẢNH - QUAN TRỌNG

**Dựa trên phân tích template và PDF mẫu:**

Phần 4.1.2 "Thiết kế tổng quan" trong PDF mẫu:
- Hình 4.2: Frontend packages (12 gói)
- Hình 4.3: Backend packages (14 gói)
- Mô tả: Liệt kê TẤT CẢ các gói chính, vai trò từng gói (1 câu), KHÔNG đi sâu vào cấu trúc bên trong

**Chiến lược cho AIChat2025:**
- ✅ **1 DIAGRAM DUY NHẤT** thể hiện toàn bộ hệ thống (Frontend + Backend + AI + Data)
- ✅ **HIGH-LEVEL**: Chỉ liệt kê packages chính, không folders chi tiết bên trong
- ✅ **NHẤN MẠNH Multi-tenant**: Đánh dấu rõ [MULTITENANT] cho mọi package liên quan
- ✅ **NHẤN MẠNH RAG**: Đánh dấu rõ [RAG] và vẽ pipeline flow
- ✅ **Dependencies quan trọng**: Cross-tier, cross-service, async messages

---

## DIAGRAM: AICHAT2025 SYSTEM ARCHITECTURE (PACKAGE DIAGRAM)

### Cấu trúc diagram (6 TIERS từ trên xuống)

```
┌────────────────────────────────────────────────────────────────┐
│                   AICHAT2025 SYSTEM PACKAGES                   │
│          Multi-tenant RAG Architecture (Package View)          │
└────────────────────────────────────────────────────────────────┘

TIER 1: PRESENTATION (Frontend Applications)
TIER 2: API GATEWAY
TIER 3: APPLICATION SERVICES (.NET Microservices)
TIER 4: AI PROCESSING (Python Services)
TIER 5: MESSAGE QUEUE
TIER 6: DATA & STORAGE
```

---

## PHÂN TÍCH TỪNG TIER

### **TIER 1: PRESENTATION LAYER (Frontend Applications)**

**Mục tiêu:** Liệt kê TẤT CẢ frontend projects

```bash
# Từ project_map.md, tìm:
# - ASP.NET MVC projects (WebApp, AdminCMS)
# - Hoặc React/Angular projects nếu có

# Với MỖI frontend project:
# 1. Tên package chính xác
# 2. Purpose (user portal / admin portal)
# 3. URL/Subdomain pattern
# 4. Có tenant-aware không?
```

**Output format:**
```markdown
## TIER 1: PRESENTATION LAYER

### Overview
Tầng giao diện người dùng, gồm các ứng dụng web chạy trên trình duyệt, giao tiếp với backend qua API Gateway thông qua HTTPS.

### Package 1.1: WebApp.Tenant

**Full Name**: `AIChat.Frontend.WebApp`
**Location**: `./src/Frontend/WebApp/WebApp.csproj`
**Type**: ASP.NET MVC Application
**Framework**: .NET 9

**Purpose**: 
Ứng dụng web dành cho người dùng cuối của từng tenant, cung cấp giao diện hỏi đáp AI, xem tài liệu, và gửi phản hồi.

**Multi-tenant**: [MULTITENANT]
- Truy cập qua subdomain riêng (tenant1.aichat.vn, tenant2.aichat.vn)
- Tự động nhận diện tenant từ subdomain
- UI hiển thị thông tin tenant (logo, tên công ty)

**RAG Features**: [RAG]
- Chat interface với streaming response
- Document viewer với highlight search results
- Feedback submission cho chat responses

**Target Users**: Nhân viên của tenant (end users)

**Key Packages** (High-level):
- `Controllers/` - MVC controllers (Home, Chat, Document, Profile)
- `Views/` - Razor views
- `Models/` - ViewModels, DTOs
- `Services/ApiClient/` - HTTP clients to API Gateway
- `wwwroot/` - Static assets (CSS, JS, images)

**Dependencies**:
- → API Gateway [HTTPS] - Calls /api/chat/*, /api/document/*
- → SignalR Hub [WebSocket] - Real-time chat streaming

---

### Package 1.2: AdminCMS.Global

**Full Name**: `AIChat.Frontend.AdminCMS`
**Location**: `./src/Frontend/AdminCMS/AdminCMS.csproj`
**Type**: ASP.NET MVC Application
**Framework**: .NET 9

**Purpose**:
Ứng dụng quản trị dành cho super admin, cho phép quản lý tất cả tenants, người dùng, tài liệu tri thức, và cấu hình hệ thống.

**Multi-tenant**: [MULTITENANT]
- Truy cập qua admin.aichat.vn (không subdomain)
- Có quyền xem và quản lý TẤT CẢ tenants
- Tenant selector trong UI để chọn tenant cần quản lý

**RAG Features**: [RAG]
- Upload/manage documents for tenants
- Configure enterprise dictionary per tenant
- Manage RAG settings (model, temperature, prompt templates)
- Trigger document reprocessing (re-chunking, re-embedding)

**Target Users**: Super Admin (1-2 người)

**Key Packages** (High-level):
- `Controllers/` - Admin controllers (Tenant, User, Document, Dictionary, Config)
- `Views/` - Admin views
- `Models/` - Admin ViewModels
- `Services/ApiClient/` - HTTP clients to API Gateway
- `wwwroot/` - Admin UI assets

**Dependencies**:
- → API Gateway [HTTPS] - Calls /api/tenant/*, /api/document/*, /api/account/*

---

### Tier 1 Statistics:
- Total Frontend Projects: 2
- [MULTITENANT] Projects: 2
- [RAG] Projects: 2
```

---

### **TIER 2: API GATEWAY**

**Mục tiêu:** Tìm Gateway package và xác định vai trò

```bash
# Từ project_map.md, tìm:
# - Gateway/YARP project
# - Có thể tên: Gateway, ApiGateway, hoặc tương tự

# Xác định:
# 1. Tên package
# 2. Middleware nào liên quan tenant?
# 3. Routing configuration
```

**Output format:**
```markdown
## TIER 2: API GATEWAY

### Overview
Cổng API duy nhất của hệ thống, đóng vai trò trung gian giữa Frontend và Backend Services.

### Package 2.1: Gateway.YARP

**Full Name**: `AIChat.Gateway`
**Location**: `./src/Gateway/Gateway.csproj`
**Type**: YARP Reverse Proxy
**Framework**: .NET 9

**Purpose**:
Nhận tất cả HTTP requests từ Frontend, thực hiện xác thực JWT, phân giải tenant từ subdomain, và định tuyến requests đến microservice phù hợp.

**Multi-tenant**: [MULTITENANT]
- **Tenant Resolution**: Trích xuất tenant từ subdomain hoặc custom header
  * tenant1.aichat.vn → TenantId = "tenant1-guid"
  * Hoặc header: X-Tenant-Id
- **Tenant Validation**: Gọi TenantService để validate tenant exists và active
- **Tenant Context Propagation**: Thêm TenantId vào header cho downstream services

**RAG**: N/A (chỉ routing, không xử lý RAG logic)

**Key Responsibilities**:
1. Authentication: Validate JWT token
2. Tenant Resolution & Validation [MULTITENANT]
3. Request Routing: Route to appropriate microservice
4. Load Balancing: Distribute requests across service instances
5. Rate Limiting: Limit requests per tenant
6. Logging & Monitoring: Log all requests

**Key Packages** (High-level):
- `Middleware/` - Custom middleware (TenantResolution, Authentication)
- `Configuration/` - YARP routes configuration
- `Extensions/` - Service registration

**Routing Rules**:
- /api/account/* → AccountService
- /api/tenant/* → TenantService
- /api/chat/* → ChatService [RAG]
- /api/document/* → DocumentService [RAG]
- /api/storage/* → StorageService

**Dependencies**:
- → TenantService [HTTP] - GET /api/tenant/validate/{subdomain} [MULTITENANT]
- → All .NET Microservices [HTTP Routing]

---

### Tier 2 Statistics:
- Total Gateway Projects: 1
- [MULTITENANT]: Yes (core functionality)
```

---

### **TIER 3: APPLICATION SERVICES (.NET Microservices)**

**Mục tiêu:** Liệt kê TẤT CẢ 5 .NET services, nhấn mạnh vai trò trong Multi-tenant và RAG

```bash
# Từ project_map.md, tìm 5 services:
# Expected: AccountService, TenantService, ChatService, DocumentService, StorageService

# Với MỖI service:
# 1. Tên chính xác
# 2. Purpose (1-2 câu)
# 3. Multi-tenant: Entities có TenantId không?
# 4. RAG: Có liên quan chat/document/embedding không?
# 5. Database name
# 6. Dependencies (cross-service calls)
```

**Output format:**
```markdown
## TIER 3: APPLICATION SERVICES (.NET Microservices)

### Overview
Tầng nghiệp vụ ứng dụng, gồm 5 microservices độc lập, mỗi service có database riêng tuân theo pattern Database-per-Service.

---

### Package 3.1: AccountService.API

**Full Name**: `AIChat.Services.AccountService`
**Location**: `./src/Services/AccountService/AccountService.csproj`
**Type**: .NET 9 WebAPI
**Framework**: .NET 9

**Purpose**:
Quản lý xác thực, người dùng, vai trò và quyền hạn. Đảm bảo mọi user operation đều tuân thủ tenant boundaries.

**Multi-tenant**: [MULTITENANT]
- **User entity** có TenantId property
- **Role entity** có TenantId property
- **Row-level security**: Mọi query tự động filter theo TenantId
- **Cross-tenant prevention**: Không thể truy cập users của tenant khác

**RAG**: N/A

**Database**: `AIChat_Account` (SQL Server)
- Tables: Users, Roles, Permissions, UserRoles (all có TenantId column)

**Key Packages** (Clean Architecture):
- `Presentation/` - Controllers, DTOs, Middleware
- `Application/` - Services, Interfaces, Validators
- `Domain/` - Entities (User, Role, Permission), Enums
- `Infrastructure/` - DbContext, Repositories, External Services
- `Shared/` - Constants, Extensions

**Dependencies**:
- → TenantService [HTTP] - Validates tenant exists before creating user [MULTITENANT]

---

### Package 3.2: TenantService.API

**Full Name**: `AIChat.Services.TenantService`
**Location**: `./src/Services/TenantService/TenantService.csproj`
**Type**: .NET 9 WebAPI
**Framework**: .NET 9

**Purpose**:
Quản lý thông tin tenant, cấu hình hệ thống theo tenant, và cung cấp validation API cho Gateway và các services khác.

**Multi-tenant**: [MULTITENANT]
- **Central authority** cho tenant information
- Provides **tenant validation API** cho Gateway
- Manages **tenant-specific configurations**

**RAG**: [RAG]
- Stores **enterprise dictionary** per tenant (for query expansion)
- Manages **RAG configuration** per tenant:
  * LLM model selection (Qwen2.5-7B, GPT-4, etc.)
  * Temperature, max_tokens settings
  * System prompt templates
  * Rerank settings

**Database**: `AIChat_Tenant` (SQL Server)
- Tables: Tenants, TenantConfigs, Dictionaries, PromptTemplates

**Key Packages**:
- `Presentation/` - Controllers, DTOs
- `Application/` - Services, Interfaces
- `Domain/` - Entities (Tenant, TenantConfig, Dictionary)
- `Infrastructure/` - DbContext, Repositories, Cache (Redis)

**Dependencies**:
- None (other services depend on this - central authority)

---

### Package 3.3: ChatService.API

**Full Name**: `AIChat.Services.ChatService`
**Location**: `./src/Services/ChatService/ChatService.csproj`
**Type**: .NET 9 WebAPI
**Framework**: .NET 9

**Purpose**:
Xử lý nghiệp vụ hỏi đáp, lưu lịch sử chat, quản lý feedback, và kết nối với RAG pipeline qua RabbitMQ.

**Multi-tenant**: [MULTITENANT]
- **Chat entity** có TenantId
- **Message entity** có TenantId
- Each tenant has **isolated chat history**
- Validates tenant context before processing

**RAG**: [RAG]
- **RAG Query Orchestration**: Receives user query, prepares ChatQueryMessage
- **Async RAG Processing**: Publishes message to RabbitMQ → ChatProcessor
- **Response Streaming**: Receives response from ChatProcessor via SignalR/SSE
- **Feedback Collection**: Stores user feedback for RAG quality improvement

**Database**: `AIChat_Chat` (SQL Server)
- Tables: Chats, Messages, Feedbacks (all có TenantId column)

**Key Packages**:
- `Presentation/` - Controllers (ChatController, FeedbackController)
- `Application/` - Services (ChatService, FeedbackService)
- `Domain/` - Entities (Chat, Message, Feedback)
- `Infrastructure/` - DbContext, RabbitMQPublisher, SignalRHub

**Dependencies**:
- → TenantService [HTTP] - Validates tenant [MULTITENANT]
- → RabbitMQ [Async Publish] - Publishes ChatQueryMessage [RAG]

---

### Package 3.4: DocumentService.API

**Full Name**: `AIChat.Services.DocumentService`
**Location**: `./src/Services/DocumentService/DocumentService.csproj`
**Type**: .NET 9 WebAPI
**Framework**: .NET 9

**Purpose**:
Quản lý tài liệu tri thức (CRUD), phát hiện cấu trúc phân cấp văn bản pháp lý, và kích hoạt xử lý embedding qua RabbitMQ.

**Multi-tenant**: [MULTITENANT]
- **Document entity** có TenantId
- Each tenant has **isolated document library**
- **Metadata isolation** per tenant

**RAG**: [RAG]
- **Document Upload**: Receives PDF uploads, stores metadata
- **Hierarchy Detection**: Detects Vietnamese legal document structure (Chương-Mục-Điều-Khoản)
- **Trigger Embedding**: Publishes DocumentProcessMessage to RabbitMQ → EmbeddingService
- **Reprocess Documents**: Allows admin to re-chunk and re-embed documents

**Database**: `AIChat_Document` (SQL Server)
- Tables: Documents, DocumentMetadata, DocumentChunks (all có TenantId)

**Key Packages**:
- `Presentation/` - Controllers (DocumentController, MetadataController)
- `Application/` - Services (DocumentService, HierarchyDetectionService)
- `Domain/` - Entities (Document, DocumentMetadata, Chunk)
- `Infrastructure/` - DbContext, RabbitMQPublisher, StorageClient

**Dependencies**:
- → TenantService [HTTP] - Validates tenant [MULTITENANT]
- → StorageService [HTTP] - Upload/download PDF files
- → RabbitMQ [Async Publish] - Publishes DocumentProcessMessage [RAG]

---

### Package 3.5: StorageService.API

**Full Name**: `AIChat.Services.StorageService`
**Location**: `./src/Services/StorageService/StorageService.csproj`
**Type**: .NET 9 WebAPI
**Framework**: .NET 9

**Purpose**:
Quản lý upload/download files, tích hợp với MinIO object storage với bucket isolation theo tenant.

**Multi-tenant**: [MULTITENANT]
- **Bucket per tenant**: Mỗi tenant có bucket riêng trong MinIO
- **Access control**: Chỉ có thể access files của tenant mình
- **Storage quota**: Giới hạn dung lượng theo tenant

**RAG**: N/A (chỉ lưu trữ files, không xử lý RAG logic)

**Database**: `AIChat_Storage` (SQL Server)
- Tables: FileMetadata (có TenantId), UploadHistory

**Key Packages**:
- `Presentation/` - Controllers (UploadController, DownloadController)
- `Application/` - Services (StorageService)
- `Domain/` - Entities (FileMetadata)
- `Infrastructure/` - MinIOClient, DbContext

**Dependencies**:
- → MinIO [Object Storage] - Upload/download files [MULTITENANT]

---

### Tier 3 Statistics:
- Total .NET Microservices: 5
- [MULTITENANT] Services: 5 (all services)
- [RAG] Services: 2 (ChatService, DocumentService)
- Total Databases: 5 (SQL Server)
```

---

### **TIER 4: AI PROCESSING (Python Services)**

**Mục tiêu:** Liệt kê 2 Python services, nhấn mạnh vai trò trong RAG pipeline

```bash
# Từ project_map.md, tìm:
# - ChatProcessor (Python)
# - EmbeddingService (Python)

# Với MỖI service:
# 1. Tên chính xác
# 2. Purpose trong RAG pipeline
# 3. Multi-tenant: Xử lý TenantId như thế nào?
# 4. External dependencies (Qdrant, Ollama, MinIO)
```

**Output format:**
```markdown
## TIER 4: AI PROCESSING (Python Services)

### Overview
Tầng xử lý AI, gồm 2 Python services chuyên trách xử lý RAG pipeline và embedding. Giao tiếp với .NET services qua RabbitMQ (async).

---

### Package 4.1: ChatProcessor

**Full Name**: `AIChat.AI.ChatProcessor`
**Location**: `./src/AI/ChatProcessor/`
**Type**: Python FastAPI Service
**Framework**: Python 3.11 + FastAPI 0.109

**Purpose**:
Xử lý RAG pipeline hoàn chỉnh từ query expansion → hybrid search → ranking → LLM generation → response cleanup.

**Multi-tenant**: [MULTITENANT]
- Receives **TenantId** trong message từ RabbitMQ
- Queries **tenant-specific Qdrant collection** (collection per tenant)
- Loads **tenant-specific dictionary** từ TenantService
- Uses **tenant-specific RAG config** (model, prompt template)

**RAG**: [RAG]
- **RAG Pipeline Steps**:
  1. **Query Expansion**: Expand abbreviations với enterprise dictionary
  2. **Intent Classification**: Classify LEGAL_ONLY / COMPANY_ONLY / BOTH / NONE
  3. **Hybrid Search**: Vector search (Qdrant) + BM25 keyword search
  4. **RRF Ranking**: Reciprocal Rank Fusion để merge results
  5. **Context Structuring**: Build hierarchical context (Chương → Mục → Điều)
  6. **LLM Generation**: Call Ollama với structured prompt
  7. **Response Cleanup**: Remove markdown, format citations

**Key Packages** (Python modules):
- `api/` - FastAPI routes
- `services/` - QueryExpander, IntentClassifier, HybridSearcher, RRFRanker, LLMGenerator
- `models/` - Pydantic models (QueryModel, SearchResult, ResponseModel)
- `clients/` - QdrantClient, OllamaClient, RabbitMQConsumer, TenantServiceClient
- `utils/` - Text processing utilities

**External Dependencies**:
- → RabbitMQ [Consume] - Consumes ChatQueryMessage [RAG]
- → Qdrant [Vector Search] - Query vectors [RAG + MULTITENANT]
- → Ollama [LLM] - Generate response [RAG]
- → TenantService [HTTP] - Load dictionary and config [RAG + MULTITENANT]

**Key Python Libraries**:
- fastapi==0.109.0
- qdrant-client==1.7.0
- sentence-transformers==2.3.1 (MiniLM-L6 for embeddings)
- langchain==0.1.0
- rank-bm25==0.2.2
- pika==1.3.2 (RabbitMQ client)

---

### Package 4.2: EmbeddingService

**Full Name**: `AIChat.AI.EmbeddingService`
**Location**: `./src/AI/EmbeddingService/`
**Type**: Python FastAPI Service
**Framework**: Python 3.11 + FastAPI 0.109

**Purpose**:
Xử lý chunking tài liệu phân cấp, enrichment ngữ cảnh, sinh vectors embedding, và lưu vào Qdrant.

**Multi-tenant**: [MULTITENANT]
- Receives **TenantId** trong message từ RabbitMQ
- Stores vectors in **tenant-specific Qdrant collection**
- Reads documents from **tenant-specific MinIO bucket**

**RAG**: [RAG]
- **Document Processing Pipeline**:
  1. **Read Document**: Download PDF từ MinIO (tenant bucket)
  2. **Hierarchical Chunking**: Chunk theo cấu trúc (Chương → Mục → Điều → Khoản)
  3. **Fulltext Context Enrichment**: Add parent context (Chương/Mục title) vào mỗi chunk
  4. **Vector Generation**: Generate 768d vectors với sentence-transformers (MiniLM-L6)
  5. **Store in Qdrant**: Save to tenant collection với metadata (hierarchy, page, tenant_id)
  6. **Update Document Status**: Notify DocumentService về completion

**Key Packages** (Python modules):
- `api/` - FastAPI routes
- `services/` - HierarchicalChunker, ContextEnricher, VectorGenerator
- `models/` - DocumentModel, ChunkModel, VectorModel
- `clients/` - QdrantClient, MinIOClient, RabbitMQConsumer, DocumentServiceClient
- `utils/` - PDF parsing, text cleaning

**External Dependencies**:
- → RabbitMQ [Consume] - Consumes DocumentProcessMessage [RAG]
- → MinIO [Object Storage] - Read PDF files [MULTITENANT]
- → Qdrant [Vector Storage] - Store embeddings [RAG + MULTITENANT]
- → DocumentService [HTTP] - Update processing status

**Key Python Libraries**:
- sentence-transformers==2.3.1
- qdrant-client==1.7.0
- minio==7.2.0
- pypdf==3.17.0
- pika==1.3.2

---

### Tier 4 Statistics:
- Total Python Services: 2
- [MULTITENANT]: 2 (both services)
- [RAG]: 2 (both services)
```

---

### **TIER 5: MESSAGE QUEUE**

**Mục tiêu:** Mô tả RabbitMQ và vai trò trong async communication

```bash
# Xác định:
# 1. Queues nào được sử dụng?
# 2. Ai publish, ai consume?
# 3. Message format
```

**Output format:**
```markdown
## TIER 5: MESSAGE QUEUE

### Overview
RabbitMQ làm message broker, cho phép giao tiếp bất đồng bộ giữa .NET services và Python services.

---

### Package 5.1: RabbitMQ.MessageBroker

**Type**: RabbitMQ Server
**Version**: 3.12

**Purpose**:
Decouples .NET services khỏi Python services, cho phép xử lý RAG bất đồng bộ và scale independently.

**Multi-tenant**: [MULTITENANT]
- Messages chứa TenantId để route đến đúng tenant data

**RAG**: [RAG]
- All RAG processing là async qua message queue

**Key Queues**:

1. **chat_query_queue** [RAG]
   - Publisher: ChatService.API (.NET)
   - Consumer: ChatProcessor (Python)
   - Message: ChatQueryMessage
     * TenantId (guid)
     * UserId (guid)
     * QueryText (string)
     * ChatId (guid)
     * Timestamp

2. **document_process_queue** [RAG]
   - Publisher: DocumentService.API (.NET)
   - Consumer: EmbeddingService (Python)
   - Message: DocumentProcessMessage
     * TenantId (guid)
     * DocumentId (guid)
     * FilePath (string in MinIO)
     * ProcessingType (enum: NEW / REPROCESS)
     * Timestamp

**Benefits**:
- **Async Processing**: User không chờ RAG processing (1-3 giây)
- **Resilience**: Nếu Python service down, messages được queue
- **Scalability**: Có thể scale Python services độc lập
- **Load Balancing**: Multiple consumers có thể process cùng queue

**Dependencies**:
- ← ChatService [Publish] - chat_query_queue
- ← DocumentService [Publish] - document_process_queue
- → ChatProcessor [Consume] - chat_query_queue
- → EmbeddingService [Consume] - document_process_queue

---

### Tier 5 Statistics:
- Total Message Brokers: 1
- Total Queues: 2
- [RAG] Queues: 2
```

---

### **TIER 6: DATA & STORAGE**

**Mục tiêu:** Liệt kê tất cả data stores và isolation strategies

```bash
# Từ project_map.md, xác định:
# 1. SQL Server databases (5 databases)
# 2. Qdrant (vector DB)
# 3. MinIO (object storage)
# 4. Ollama (LLM runtime)

# Với MỖI data store:
# 1. Purpose
# 2. Multi-tenant isolation strategy
# 3. RAG role
```

**Output format:**
```markdown
## TIER 6: DATA & STORAGE

### Overview
Tầng dữ liệu và lưu trữ, bao gồm relational database, vector database, object storage, và LLM runtime.

---

### Package 6.1: SQLServer.Databases

**Type**: SQL Server 2022
**Purpose**: Lưu trữ relational data cho tất cả .NET microservices

**Multi-tenant**: [MULTITENANT]
- **Isolation Strategy**: Row-level security
- **Implementation**: Mỗi table có cột TenantId (guid)
- **Query Filtering**: EF Core global query filter tự động thêm WHERE TenantId = @CurrentTenantId
- **Index**: Clustered index trên (TenantId, Id) cho performance

**Databases**:
1. `AIChat_Account` - Users, Roles, Permissions
2. `AIChat_Tenant` - Tenants, Configs, Dictionaries
3. `AIChat_Chat` - Chats, Messages, Feedbacks
4. `AIChat_Document` - Documents, Metadata, Chunks
5. `AIChat_Storage` - FileMetadata, UploadHistory

**Key Tables with TenantId**:
- Users (TenantId, Id, Email, ...)
- Roles (TenantId, Id, Name, ...)
- Chats (TenantId, Id, UserId, ...)
- Messages (TenantId, Id, ChatId, Content, ...)
- Documents (TenantId, Id, Title, FilePath, ...)

**RAG**: N/A (chỉ lưu metadata, không lưu vectors)

---

### Package 6.2: Qdrant.VectorDatabase

**Type**: Qdrant Vector Database
**Version**: 1.7

**Purpose**: Lưu trữ document embeddings (768d vectors) cho semantic search trong RAG pipeline.

**Multi-tenant**: [MULTITENANT]
- **Isolation Strategy**: Collection per tenant
- **Implementation**: Mỗi tenant có collection riêng
  * Collection name: `tenant_{tenantId}`
  * Example: `tenant_12345678-1234-1234-1234-123456789abc`
- **Access Control**: Python services chỉ query collection của tenant hiện tại

**RAG**: [RAG]
- **Vector Storage**: Stores 768d embeddings (MiniLM-L6)
- **Semantic Search**: ANN search với HNSW index
- **Metadata**: Stores chunk hierarchy (chapter, section, article), page number, document_id

**Vector Schema per Tenant Collection**:
```python
{
    "vector": [0.123, ...],  # 768 dimensions
    "payload": {
        "tenant_id": "guid",
        "document_id": "guid",
        "chunk_id": "guid",
        "text": "chunk content",
        "hierarchy": {
            "chapter": "Chương I",
            "section": "Mục 1",
            "article": "Điều 5"
        },
        "page": 10,
        "chunk_type": "ARTICLE"  # CHAPTER / SECTION / ARTICLE / CLAUSE
    }
}
```

**Index**: HNSW (Hierarchical Navigable Small World) for fast ANN search

**Dependencies**:
- ← EmbeddingService [Write] - Store vectors
- ← ChatProcessor [Read] - Semantic search

---

### Package 6.3: MinIO.ObjectStorage

**Type**: MinIO Object Storage
**Version**: Latest

**Purpose**: Lưu trữ PDF files (tài liệu gốc) với bucket isolation theo tenant.

**Multi-tenant**: [MULTITENANT]
- **Isolation Strategy**: Bucket per tenant
- **Implementation**: Mỗi tenant có bucket riêng
  * Bucket name: `tenant-{tenantId}`
  * Example: `tenant-12345678-1234-1234-1234-123456789abc`
- **Access Control**: Pre-signed URLs với expiration
- **Storage Quota**: Có thể giới hạn bucket size theo tenant plan

**RAG**: [RAG]
- **Document Storage**: Stores original PDF files
- **Access by AI**: EmbeddingService reads PDFs từ tenant bucket để chunk và embed

**Bucket Structure per Tenant**:
```
tenant-{tenantId}/
├── documents/
│   ├── {documentId}.pdf
│   ├── {documentId}.pdf
│   └── ...
└── uploads/
    └── temp files
```

**Dependencies**:
- ← StorageService [Write] - Upload files
- ← EmbeddingService [Read] - Read PDFs for processing

---

### Package 6.4: Ollama.LLMRuntime

**Type**: Ollama LLM Runtime
**Version**: Latest

**Purpose**: Local LLM inference runtime, chạy Qwen2.5-7B model cho RAG generation.

**Multi-tenant**: N/A
- Shared LLM runtime for all tenants
- No tenant-specific models (cost-prohibitive)
- Tenant-specific: Only prompt templates and configs

**RAG**: [RAG]
- **LLM Generation**: Generates answers based on retrieved context
- **Model**: Qwen2.5-7B-Instruct (quantized GGUF)
- **Inference**: GPU-accelerated (NVIDIA RTX 3090 / 4090)
- **API**: OpenAI-compatible API (http://localhost:11434/v1/chat/completions)

**Generation Settings** (per tenant config):
- Temperature: 0.1 - 0.9 (configurable)
- Max tokens: 512 - 2048 (configurable)
- Top-p: 0.9
- Frequency penalty: 0.0

**Dependencies**:
- ← ChatProcessor [API Call] - Generate responses

---

### Tier 6 Statistics:
- Total Data Stores: 4
- SQL Databases: 5
- Vector Databases: 1 (multi-collection)
- Object Storage: 1 (multi-bucket)
- LLM Runtime: 1
- [MULTITENANT] Stores: 3 (SQL, Qdrant, MinIO)
- [RAG] Stores: 3 (Qdrant, MinIO, Ollama)
```

---

## CROSS-TIER DEPENDENCIES (Vẽ rõ trên diagram)

### **Multi-tenant Dependencies** (màu đỏ)

```markdown
### Multi-tenant Flow:

1. **Tenant Resolution & Validation** [MULTITENANT]
   ```
   Frontend.WebApp → API Gateway
       Gateway extracts TenantId from subdomain
       Gateway → TenantService.ValidateTenant(subdomain)
       TenantService returns TenantDto
       Gateway propagates TenantId to downstream services
   ```

2. **Cross-Service Tenant Validation** [MULTITENANT]
   ```
   AccountService.CreateUser → TenantService.GetTenant(tenantId)
       Purpose: Ensure tenant exists before creating user
   
   ChatService.CreateChat → TenantService.GetConfig(tenantId)
       Purpose: Load tenant RAG config
   
   DocumentService.UploadDocument → TenantService.ValidateTenant(tenantId)
       Purpose: Ensure tenant exists before storing document
   ```

3. **Data Isolation** [MULTITENANT]
   ```
   All .NET Services → SQL Server
       Query Filter: WHERE TenantId = @CurrentTenantId
   
   ChatProcessor → Qdrant
       Collection: tenant_{tenantId}
   
   EmbeddingService → MinIO
       Bucket: tenant-{tenantId}
   ```
```

### **RAG Pipeline Dependencies** (màu xanh lá)

```markdown
### RAG Flow:

1. **Chat Query Flow** [RAG]
   ```
   User Query
       ↓
   Frontend.WebApp.ChatController
       ↓ [HTTPS POST /api/chat/query]
   API Gateway
       ↓ [Route to ChatService]
   ChatService.CreateChat
       ↓ [Save chat + Publish ChatQueryMessage]
   RabbitMQ.chat_query_queue
       ↓ [Consume message]
   ChatProcessor.ProcessQuery
       ↓ Steps:
       1. Load tenant dictionary from TenantService [HTTP]
       2. Expand query with dictionary
       3. Classify intent (LEGAL_ONLY/COMPANY_ONLY/BOTH)
       4. Hybrid search: Qdrant (vector) + BM25 (keyword)
       5. RRF ranking
       6. Structure context (hierarchical)
       7. Call Ollama LLM [HTTP] → Generate response
       8. Cleanup response
       ↓ [Send back via SignalR/SSE]
   Frontend.WebApp (displays streaming response)
   ```

2. **Document Processing Flow** [RAG]
   ```
   Admin uploads PDF
       ↓
   AdminCMS.DocumentController
       ↓ [HTTPS POST /api/document/upload]
   API Gateway
       ↓ [Route to DocumentService]
   DocumentService.UploadDocument
       ↓ [Upload to MinIO via StorageService]
   StorageService → MinIO (store in tenant bucket)
       ↓ [Publish DocumentProcessMessage]
   RabbitMQ.document_process_queue
       ↓ [Consume message]
   EmbeddingService.ProcessDocument
       ↓ Steps:
       1. Download PDF from MinIO (tenant bucket)
       2. Detect hierarchy (Chương-Mục-Điều-Khoản)
       3. Hierarchical chunking
       4. Fulltext context enrichment
       5. Generate 768d vectors (MiniLM-L6)
       6. Store vectors in Qdrant (tenant collection)
       7. Update document status in DocumentService [HTTP]
       ↓
   DocumentService.UpdateStatus(Enriched)
   ```
```

---

## DIAGRAM LAYOUT (Text-based Structure for Draw.io)

```
┌──────────────────────────────────────────────────────────────────────┐
│                   TIER 1: PRESENTATION LAYER                         │
│                                                                      │
│   ┌──────────────────┐                    ┌──────────────────┐     │
│   │ WebApp.Tenant    │                    │ AdminCMS.Global  │     │
│   │ [MULTITENANT]    │                    │ [MULTITENANT]    │     │
│   │ [RAG]            │                    │ [RAG]            │     │
│   └──────────────────┘                    └──────────────────┘     │
└──────────────────────────────────────────────────────────────────────┘
                  │                                   │
                  │ HTTPS                             │ HTTPS
                  └───────────────┬───────────────────┘
                                  ▼
┌──────────────────────────────────────────────────────────────────────┐
│                   TIER 2: API GATEWAY                                │
│                                                                      │
│   ┌──────────────────────────────────────────────────────────┐     │
│   │              Gateway.YARP [MULTITENANT]                  │     │
│   │  • Tenant Resolution • Authentication • Routing          │     │
│   └──────────────────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────────────────┘
                                  │
           ┌──────────────────────┼──────────────────────┬─────────┐
           │                      │                      │         │
           ▼                      ▼                      ▼         ▼
┌──────────────────────────────────────────────────────────────────────┐
│               TIER 3: APPLICATION SERVICES (.NET 9)                  │
│                                                                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────┐│
│  │Account   │  │Tenant    │  │Chat      │  │Document  │  │Storage││
│  │Service   │  │Service   │  │Service   │  │Service   │  │Service││
│  │[MULTI]   │  │[MULTI]   │  │[M][RAG]  │  │[M][RAG]  │  │[MULTI]││
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────┘│
└──────────────────────────────────────────────────────────────────────┘
                                     │              │
                                     │ Async (RabbitMQ)
                                     ▼              ▼
┌──────────────────────────────────────────────────────────────────────┐
│                   TIER 5: MESSAGE QUEUE                              │
│                                                                      │
│   ┌────────────────────────────────────────────────────────┐       │
│   │              RabbitMQ [RAG]                            │       │
│   │  Queue: chat_query_queue, document_process_queue       │       │
│   └────────────────────────────────────────────────────────┘       │
└──────────────────────────────────────────────────────────────────────┘
                                     │
                         ┌───────────┴───────────┐
                         ▼                       ▼
┌──────────────────────────────────────────────────────────────────────┐
│               TIER 4: AI PROCESSING (Python 3.11)                    │
│                                                                      │
│   ┌──────────────────┐                  ┌──────────────────┐       │
│   │ ChatProcessor    │                  │ EmbeddingService │       │
│   │ [MULTITENANT]    │                  │ [MULTITENANT]    │       │
│   │ [RAG]            │                  │ [RAG]            │       │
│   └──────────────────┘                  └──────────────────┘       │
└──────────────────────────────────────────────────────────────────────┘
           │                                        │
           │ Vector Search / Store                  │ Object Storage
           ▼                                        ▼
┌──────────────────────────────────────────────────────────────────────┐
│                   TIER 6: DATA & STORAGE                             │
│                                                                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐           │
│  │SQL Server│  │Qdrant    │  │MinIO     │  │Ollama    │           │
│  │5 DBs     │  │Vector DB │  │Object    │  │LLM       │           │
│  │[MULTI]   │  │[M][RAG]  │  │[M][RAG]  │  │[RAG]     │           │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘           │
└──────────────────────────────────────────────────────────────────────┘

Legend:
[MULTI] = [MULTITENANT]
[RAG] = RAG-related
─────▶ = Sync (HTTP/HTTPS)
⤏⤏⤏⤏▶ = Async (RabbitMQ)
```

---

## OUTPUT FORMAT

Tạo file markdown: `package_analysis_4.1.2.md` với cấu trúc:

```markdown
# AICHAT2025 - PACKAGE DIAGRAM ANALYSIS (4.1.2)

## Overview
Phân tích packages cho 1 diagram duy nhất thể hiện toàn bộ hệ thống với 6 tiers.

## TIER 1: PRESENTATION LAYER
[Chi tiết như format trên]

## TIER 2: API GATEWAY
[Chi tiết như format trên]

## TIER 3: APPLICATION SERVICES
[Chi tiết 5 services như format trên]

## TIER 4: AI PROCESSING
[Chi tiết 2 Python services như format trên]

## TIER 5: MESSAGE QUEUE
[Chi tiết RabbitMQ như format trên]

## TIER 6: DATA & STORAGE
[Chi tiết 4 data stores như format trên]

## CROSS-TIER DEPENDENCIES
[Multi-tenant flow + RAG pipeline flow]

## DIAGRAM LAYOUT
[Text-based structure for Draw.io]

## STATISTICS
- Total Tiers: 6
- Total Packages: 14 (2 frontend + 1 gateway + 5 .NET + 2 Python + 4 data)
- [MULTITENANT] Packages: 12
- [RAG] Packages: 7
- Cross-tier Dependencies: 15+
```

---

## YÊU CẦU QUAN TRỌNG

1. ✅ **HIGH-LEVEL**: Chỉ liệt kê packages chính, không folders chi tiết bên trong
2. ✅ **NHẤN MẠNH**: Đánh dấu rõ ràng [MULTITENANT] và [RAG] cho MỌI package
3. ✅ **PURPOSE**: Mỗi package có mô tả vai trò ngắn gọn (1-2 câu)
4. ✅ **DEPENDENCIES**: Chỉ liệt kê dependencies QUAN TRỌNG (cross-service, async)
5. ✅ **TEXT-BASED**: Output là text-based tree, KHÔNG dùng PlantUML/Mermaid
6. ✅ **CHÍNH XÁC**: Dựa 100% vào project_map.md, không tưởng tượng

---

## BẮT ĐẦU PHÂN TÍCH

Hãy:
1. Đọc kỹ `project_map.md`
2. Phân tích từng tier một, từ TIER 1 → TIER 6
3. Với mỗi package, xác định: Purpose + [MULTITENANT]? + [RAG]? + Dependencies
4. Tạo text-based diagram layout
5. Xuất ra file `package_analysis_4.1.2.md` hoàn chỉnh

BẮT ĐẦU với TIER 1 (Presentation Layer).
