# CHƯƠNG 4: THIẾT KẾ VÀ TRIỂN KHAI HỆ THỐNG

**Mục đích:** Trình bày thiết kế kiến trúc, cơ sở dữ liệu, và chi tiết triển khai các thành phần hệ thống

**Số trang ước tính:** 30-35 trang

**Lưu ý quan trọng:**
- ⚠️ Chương 5 đã trình bày chi tiết kiến trúc hệ thống. Chương này sẽ tóm tắt và tham chiếu Chương 5, tập trung vào **quyết định thiết kế** và **quá trình triển khai**
- Tránh lặp lại nội dung Chương 5

---

## 4.1. Tổng quan kiến trúc hệ thống

**Nội dung chính:**

### 4.1.1. Lựa chọn kiến trúc

**Quyết định:** Microservices Architecture

**Lý do:**
1. **Polyglot programming:** Backend (.NET) + AI workers (Python) - mỗi ngôn ngữ phù hợp với công việc riêng
2. **Independent deployment:** Deploy từng service riêng, không ảnh hưởng toàn hệ thống
3. **Scalability:** Scale AI workers riêng khi load tăng
4. **Technology diversity:** SQL Server cho business data, Qdrant cho vectors, MinIO cho files

**Alternative considered:**
- ❌ **Monolithic:** Không phù hợp với .NET + Python, khó scale
- ❌ **Serverless:** Không phù hợp self-hosted requirement

### 4.1.2. C4 Model - System Context

**Mô tả hệ thống ở mức cao nhất:**

```
┌─────────────────────────────────────────────────────┐
│                   USERS                              │
│  - Nhân viên (Employee)                             │
│  - HR/Admin                                         │
└─────────────────┬───────────────────────────────────┘
                  │ HTTPS / WebSocket
                  ↓
┌─────────────────────────────────────────────────────┐
│         AIChat2025 - Legal Chatbot System          │
│   Multi-tenant RAG-based Legal Advisory System     │
└─────────────────┬───────────────────────────────────┘
                  │ Integrates with:
                  ↓
┌─────────────────────────────────────────────────────┐
│          EXTERNAL SYSTEMS (none in MVP)             │
│  - Future: Email service (SendGrid)                 │
│  - Future: SSO (Azure AD)                           │
└─────────────────────────────────────────────────────┘
```

**Sơ đồ chi tiết:** `diagrams_to_create.md` → Diagram 4.1 (System Context C4)

**Actors:**
- **Employee (Nhân viên):** Hỏi chatbot về quy định công ty, luật lao động
- **HR/Admin:** Upload tài liệu, quản lý cấu hình, xem dashboard

**External Systems:**
- Hiện tại: None (self-contained system)
- Tương lai: Email notifications, SSO integration

### 4.1.3. C4 Model - Container Diagram

**Chi tiết về kiến trúc:** Xem **Mục 5.1** (Kiến trúc hệ thống tổng quan)

**Tóm tắt 9 containers chính:**

**Frontend:**
1. **WebApp** (ASP.NET MVC): Giao diện người dùng

**API Gateway:**
2. **ApiGateway** (YARP): Reverse proxy, routing

**Backend Microservices (.NET):**
3. **AccountService**: Authentication, user management
4. **TenantService**: Multi-tenant management
5. **DocumentService**: Document upload, background vectorization (Hangfire)
6. **StorageService**: MinIO integration (file storage)
7. **ChatService**: SignalR hub, chat orchestration, RabbitMQ integration

**AI Workers (Python):**
8. **EmbeddingService** (FastAPI): Text embedding, Qdrant integration
9. **ChatProcessor** (FastAPI): RAG pipeline, LLM generation

**Infrastructure (Docker containers):**
- SQL Server 2022
- Qdrant
- RabbitMQ
- MinIO
- Ollama

**Sơ đồ chi tiết:** Xem Mục 5.1.2 hoặc `diagrams_to_create.md` → Diagram 4.2

### 4.1.4. Communication Patterns

**3 loại giao tiếp:**

**1. Synchronous HTTP (REST API):**
```
WebApp → ApiGateway → AccountService (GET /api/account/current-user)
DocumentService → EmbeddingService (POST /vectorize-batch)
```

**2. Asynchronous Messaging (RabbitMQ):**
```
ChatService → RabbitMQ → ChatProcessor
(UserPromptReceivedEvent → BotResponseCreatedEvent)
```

**3. Real-time (SignalR WebSocket):**
```
WebApp ↔ ChatService ↔ WebApp
(Bidirectional communication for chat)
```

**Quyết định thiết kế:**
- HTTP cho CRUD operations (stateless, RESTful)
- RabbitMQ cho long-running AI tasks (decoupling, retry mechanism)
- SignalR cho real-time user experience (WebSocket)

**Chi tiết:** Xem Mục 5.1.4 (Service communication)

---

## 4.2. Thiết kế cơ sở dữ liệu

**Nội dung chính:**

### 4.2.1. Database Schema Design

**Kiến trúc database:** Xem **Mục 5.2.2** (Database schema chi tiết)

**Tóm tắt 8 tables chính:**

**1. Accounts (Tài khoản người dùng):**
```sql
CREATE TABLE Accounts (
    Id INT PRIMARY KEY IDENTITY,
    TenantId INT NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    FullName NVARCHAR(255),
    Role NVARCHAR(50),  -- 'admin' hoặc 'user'
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT UK_Account_Tenant_Email UNIQUE (TenantId, Email)
);
```

**2. Tenants (Công ty/Tổ chức):**
```sql
CREATE TABLE Tenants (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
```

**3. ChatConversations (Cuộc trò chuyện):**
```sql
CREATE TABLE ChatConversations (
    Id INT PRIMARY KEY IDENTITY,
    TenantId INT NOT NULL,
    UserId INT NOT NULL,
    Title NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Accounts(Id)
);
```

**4. ChatMessages (Tin nhắn trong cuộc trò chuyện):**
```sql
CREATE TABLE ChatMessages (
    Id INT PRIMARY KEY IDENTITY,
    ConversationId INT NOT NULL,
    IsBot BIT NOT NULL,  -- TRUE = bot, FALSE = user
    Content NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (ConversationId) REFERENCES ChatConversations(Id)
);
```

**5. PromptDocuments (Tài liệu đã upload):**
```sql
CREATE TABLE PromptDocuments (
    Id INT PRIMARY KEY IDENTITY,
    TenantId INT NOT NULL,
    DocumentName NVARCHAR(500) NOT NULL,
    FileName NVARCHAR(500) NOT NULL,
    Status NVARCHAR(50),  -- 'Pending', 'Processing', 'Completed', 'Failed'
    IsCompanyRule BIT DEFAULT 0,  -- TRUE = quy định công ty, FALSE = luật nhà nước
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
```

**6. PromptConfigs (Cấu hình prompt):**
```sql
CREATE TABLE PromptConfigs (
    Id INT PRIMARY KEY IDENTITY,
    TenantId INT NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    SystemPrompt NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL
);
```

**7. Permissions (Phân quyền - placeholder):**
```sql
CREATE TABLE Permissions (
    Id INT PRIMARY KEY IDENTITY,
    TenantId INT NOT NULL,
    UserId INT NOT NULL,
    PermissionName NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
```

**ER Diagram:** `diagrams_to_create.md` → Diagram 4.7 (Database ER Diagram)

### 4.2.2. Multi-tenant Row-Level Security

**Quyết định thiết kế:** Shared Database, Shared Schema với Row-level isolation

**Implementation:** Xem **Mục 5.2.1** (Multi-tenant implementation chi tiết)

**Tóm tắt cách triển khai:**

**1. BaseEntity với TenantId:**
```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**2. UpdateTenancyInterceptor (EF Core Interceptor):**
```csharp
public class UpdateTenancyInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, ...)
    {
        var tenantId = _currentTenantProvider.GetTenantId();

        foreach (var entry in eventData.Context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantId;
            }
        }

        return base.SavingChanges(eventData, result);
    }
}
```

**3. TenancySpecification (Query filtering):**
```csharp
var spec = new TenancySpecification<Account>(_currentTenantProvider.GetTenantId());
var accounts = await _repository.ListAsync(spec);
// SELECT * FROM Accounts WHERE TenantId = {currentTenantId}
```

**Ưu điểm:**
- ✅ Chi phí thấp (1 database duy nhất)
- ✅ Dễ quản lý migrations
- ✅ Tự động filtering với Interceptor + Specification

**Rủi ro:**
- ⚠️ Lỗi code có thể leak data giữa tenants
- **Giải pháp:** Unit tests, code review

**Lưu ý đặc biệt:**
- Legal base (luật nhà nước) có `TenantId = 1` (shared across all tenants)
- Company rules có `TenantId = {specific tenant}` (isolated per tenant)

### 4.2.3. Qdrant Collection Schema

**Quyết định:** 1 collection duy nhất `vn_law_documents` cho cả legal base và company rules

**Schema:**
```json
{
  "vector_size": 768,
  "distance": "Cosine",
  "payload_schema": {
    "text": "string (indexed)",
    "document_name": "string (indexed)",
    "heading1": "string",
    "heading2": "string",
    "father_doc_name": "string",
    "tenant_id": "integer (indexed, filterable)"
  }
}
```

**Indexing:**
- `tenant_id`: Indexed for fast filtering
- `document_name`: Indexed for metadata search

**Vector search với tenant filtering:**
```python
results = qdrant_client.search(
    collection_name="vn_law_documents",
    query_vector=query_embedding,
    query_filter={
        "must": [
            {"key": "tenant_id", "match": {"value": tenant_id}}
        ]
    },
    limit=5
)
```

**Alternative considered:**
- ❌ **Separate collections per tenant:** Phức tạp quản lý, không cần thiết
- ✅ **Single collection with filtering:** Đơn giản, hiệu quả

---

## 4.3. Thiết kế và triển khai các microservices

**Nội dung chính:**

### 4.3.1. AccountService - Authentication

**Chức năng chính:**
- Đăng ký, đăng nhập
- JWT token generation
- User management (CRUD)

**Endpoints chính:** Xem `code_statistics.json` → backend_dotnet → AccountService

**Quyết định thiết kế:**

**1. Password hashing với BCrypt:**
```csharp
public string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
}

public bool VerifyPassword(string password, string hash)
{
    return BCrypt.Net.BCrypt.Verify(password, hash);
}
```

**Lý do:** BCrypt > MD5/SHA256 (adaptive cost, salt tự động)

**2. JWT Token Structure:**
```json
{
  "sub": "123",  // UserId
  "tenant_id": "1",
  "email": "user@example.com",
  "role": "admin",
  "scope": "scope_web",
  "exp": 1735479600,  // Expiry timestamp
  "iss": "AIChat2025",
  "aud": "AIChat2025"
}
```

**Implementation:** Xem `Infrastructure/Authentication/TokenClaimsService.cs`

**3. Hybrid Authentication:**
- **Web:** Cookie-based (HttpOnly, Secure)
- **Mobile (future):** JWT Bearer token

**Sequence Diagram:** `diagrams_to_create.md` → Diagram 4.5 (Authentication Sequence)

### 4.3.2. TenantService - Multi-tenant Management

**Chức năng chính:**
- Tạo tenant mới
- Liệt kê tenants
- Quản lý permissions (placeholder)

**Quyết định thiết kế:**

**Seed Data:**
```csharp
// Tenant #1 = Legal base (shared)
modelBuilder.Entity<Tenant>().HasData(new Tenant
{
    Id = 1,
    Name = "Legal Base",
    IsActive = true
});
```

**Lý do:** Legal base documents cần được chia sẻ cho tất cả tenants

### 4.3.3. DocumentService - Document Management

**Chức năng chính:** Xem **Mục 5.3** (Document processing pipeline chi tiết)

**Tóm tắt workflow:**

**1. Document Upload:**
```
User uploads .docx
    ↓
DocumentService receives file
    ↓
Save file to MinIO (StorageService API)
    ↓
Create PromptDocument record (Status = 'Pending')
    ↓
Enqueue Hangfire background job
    ↓
Return success to user
```

**2. Background Vectorization (Hangfire Job):**
```
VectorizeBackgroundJob triggered
    ↓
Update status to 'Processing'
    ↓
[Step 1] Download file from MinIO
    ↓
[Step 2] Parse .docx (DocumentFormat.OpenXml)
    ↓
[Step 3] Hierarchical chunking (preserve Chương/Điều structure)
    ↓
[Step 4] Call EmbeddingService API (batch embedding)
    ↓
[Step 5] Store vectors in Qdrant (with metadata)
    ↓
Update status to 'Completed'
```

**Quyết định thiết kế:**

**1. Background processing:**
- Lý do: Vectorization tốn thời gian (1-5 phút cho 200 trang)
- Không thể block HTTP request

**2. Hierarchical semantic chunking:**
```csharp
// Preserve document structure
class Chunk
{
    string Text { get; set; }
    string DocumentName { get; set; }
    string Heading1 { get; set; }  // "Chương XV"
    string Heading2 { get; set; }  // "Điều 24"
    string FatherDocName { get; set; }
}
```

**Lý do:** Cần metadata để trích dẫn chính xác (xem fix citation ở đầu conversation)

**Sequence Diagram:** `diagrams_to_create.md` → Diagram 4.6 (Document Embedding Sequence)

**Implementation details:** Xem `Services/DocumentService/Features/PromptDocumentBusiness.cs:100-150`

### 4.3.4. ChatService - Chat Orchestration

**Chức năng chính:** Xem **Mục 5.4** (Real-time communication chi tiết)

**Tóm tắt architecture:**

**Component diagram:**
```
WebApp (SignalR Client)
    ↕ WebSocket
ChatHub (SignalR Server)
    ↕
ChatBusiness
    ↓ Publish event
RabbitMQ (UserPromptReceivedEvent)
    ↓ Consume
ChatProcessor (Python)
    ↓ Publish response
RabbitMQ (BotResponseCreatedEvent)
    ↓ Consume
BotResponseConsumer
    ↓ Broadcast
ChatHub → WebApp
```

**Quyết định thiết kế:**

**1. Tại sao dùng RabbitMQ thay vì gọi trực tiếp ChatProcessor?**
- ✅ **Decoupling:** ChatService không phụ thuộc vào ChatProcessor availability
- ✅ **Retry mechanism:** RabbitMQ tự động retry nếu ChatProcessor down
- ✅ **Queue management:** Xử lý backlog khi nhiều requests đồng thời

**2. SignalR Groups:**
```csharp
// Join conversation group
await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());

// Broadcast to all users in conversation
await Clients.Group(conversationId.ToString()).SendAsync("BotResponse", message);
```

**Lý do:** Chỉ gửi response cho users trong conversation đó

**Class Diagram:** `diagrams_to_create.md` → Diagram 4.9 (Class Diagram - Chat)

### 4.3.5. EmbeddingService - Text Embedding (Python)

**Chức năng chính:**
- Embed text thành vector (768-dim)
- Vectorize batch documents
- Store/search trong Qdrant

**API endpoints:** Xem `code_statistics.json` → ai_workers_python → EmbeddingService

**Implementation:**
```python
from sentence_transformers import SentenceTransformer

class EmbeddingService:
    def __init__(self):
        self.model = SentenceTransformer('truro7/vn-law-embedding')

    def encode_text(self, text: str) -> np.ndarray:
        return self.model.encode(text, show_progress_bar=False)

    async def vectorize_batch(self, chunks: List[Chunk]):
        texts = [chunk['text'] for chunk in chunks]
        vectors = self.model.encode(texts, batch_size=32)

        # Store in Qdrant
        points = [
            PointStruct(
                id=str(uuid.uuid4()),
                vector=vector.tolist(),
                payload={
                    "text": chunk['text'],
                    "document_name": chunk['document_name'],
                    "heading1": chunk.get('heading1', ''),
                    "heading2": chunk.get('heading2', ''),
                    "tenant_id": chunk['tenant_id']
                }
            )
            for chunk, vector in zip(chunks, vectors)
        ]

        await qdrant_client.upsert(
            collection_name="vn_law_documents",
            points=points
        )
```

**Quyết định thiết kế:**

**1. Batch processing:**
- Embed 32 chunks cùng lúc → Nhanh hơn từng chunk
- GPU/CPU sử dụng hiệu quả hơn

**2. Metadata storage:**
- Store `document_name`, `heading1`, `heading2` → Cần cho citation (xem fix ở đầu conversation)

**File reference:** `Services/EmbeddingService/src/business.py:40-80`

### 4.3.6. ChatProcessor - RAG Pipeline (Python)

**Chức năng chính:** Xem **Mục 5.5** (RAG pipeline 9 bước chi tiết)

**Tóm tắt RAG pipeline:**

```
[1] Receive user prompt from RabbitMQ
    ↓
[2] Embed user query (768-dim vector)
    ↓
[3] Dual-RAG search:
    - Search company rules (tenant-specific)
    - Search legal base (tenant_id=1)
    ↓
[4] Scenario determination:
    - COMPANY_ONLY: Chỉ có company results
    - LEGAL_ONLY: Chỉ có legal results
    - COMPARISON: Có cả 2
    ↓
[5] Context structuring (with citations)
    ↓
[6] Prompt construction (system + context + query)
    ↓
[7] LLM generation (Ollama + Vistral)
    ↓
[8] Response cleanup (remove instruction leakage)
    ↓
[9] Publish response to RabbitMQ + Log RAGAS metrics
```

**Quyết định thiết kế:**

**1. Dual-RAG:**
```python
# Parallel search
company_results, legal_results = await asyncio.gather(
    qdrant_service.search(query_vector, tenant_id=tenant_id),
    qdrant_service.search(query_vector, tenant_id=1)  # Legal base
)
```

**Lý do:** Cần kết hợp quy định công ty + luật nhà nước

**2. Citation building (FIX applied ở đầu conversation):**
```python
def _build_citation_label(result, is_company_rule: bool, index: int) -> str:
    document_name = result.payload.get('document_name', '')
    heading1 = result.payload.get('heading1', '')
    heading2 = result.payload.get('heading2', '')

    if document_name:
        if heading1 and heading2:
            return f"[{document_name} - {heading1} - {heading2}]"
        elif heading1:
            return f"[{document_name} - {heading1}]"
        else:
            return f"[{document_name}]"
    else:
        return f"[Quy định #{index}]" if is_company_rule else f"[Văn bản #{index}]"
```

**Lý do:** Trích dẫn chính xác thay vì generic "Document #1"

**3. System prompts cho 3 scenarios:**

**COMPANY_ONLY:**
```python
return """CHỈ IN CÂU TRẢ LỜI CUỐI CÙNG.
Trả lời ngắn gọn theo mẫu: Theo [Trích dẫn chính xác từ context], [nội dung].
YÊU CẦU: Sao chép CHÍNH XÁC nhãn trích dẫn trong [...] từ context đã cung cấp."""
```

**COMPARISON:**
```python
return """So sánh quy định công ty với luật nhà nước.
Format:
1. [Kết luận]: Hợp pháp / Không hợp pháp / Cần xem xét
2. [Quy định công ty]: ...
3. [Luật nhà nước]: ...
4. [Phân tích]: ...
5. [Khuyến nghị]: ..."""
```

**4. Cleanup để tránh instruction leakage:**
```python
# Multi-pass prefix removal
prefixes_to_remove = [
    "Trích dẫn chính xác từ ngữ cảnh và trả lời câu hỏi của người dùng",
    "Dựa trên thông tin được cung cấp",
    # ... 20+ patterns
]

for _ in range(5):
    for prefix in prefixes_to_remove:
        if cleaned.startswith(prefix):
            cleaned = cleaned[len(prefix):].strip()
```

**Lý do:** LLM đôi khi repeat instruction text trong response

**Sequence Diagram:** `diagrams_to_create.md` → Diagram 4.4 (RAG Pipeline Sequence)

**File reference:** `Services/ChatProcessor/src/business.py:150-250`

---

## 4.4. Thiết kế giao diện người dùng

**Nội dung chính:**

### 4.4.1. Frontend Architecture

**Technology stack:**
- ASP.NET Core MVC 9.0 (Razor views)
- Bootstrap 5 (responsive UI)
- jQuery 3.x (DOM manipulation)
- SignalR JavaScript Client 8.0.0 (WebSocket)

**Quyết định thiết kế:**

**1. Server-side rendering (Razor) vs Client-side (React/Vue):**
- ✅ **Razor:** Đơn giản, ít setup, phù hợp thesis timeline
- ❌ **React/Vue:** Cần build process, phức tạp hơn

**2. Layout structure:**
```
_Layout.cshtml (Master page)
    ├─ Header (Navigation bar)
    ├─ Main Content (Yield @RenderBody())
    └─ Footer
```

### 4.4.2. Chat Interface Design

**UI Components:**

**1. Conversation list (Sidebar):**
```html
<div class="sidebar">
    <button id="newConversation">New Conversation</button>
    <ul id="conversationList">
        <!-- Populated via AJAX -->
    </ul>
</div>
```

**2. Chat area:**
```html
<div class="chat-area">
    <div id="messages">
        <!-- Messages rendered here -->
    </div>
    <form id="chatForm">
        <input type="text" id="userInput" placeholder="Ask about company rules or labor law..." />
        <button type="submit">Send</button>
    </form>
</div>
```

**3. Message rendering:**
```javascript
function displayMessage(message) {
    const messageDiv = $('<div>')
        .addClass(message.isBot ? 'message bot' : 'message user')
        .html(`
            <strong>${message.isBot ? 'Bot' : 'You'}:</strong>
            <p>${escapeHtml(message.content)}</p>
            <small>${new Date(message.createdAt).toLocaleTimeString()}</small>
        `);
    $('#messages').append(messageDiv);
    scrollToBottom();
}
```

**Screenshots:** Xem `assets_checklist.md` → Screenshots section

### 4.4.3. SignalR Client Integration

**Connection management:**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub", {
        accessTokenFactory: () => getCookie("AuthToken")
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000])  // Retry intervals
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.onreconnecting(() => {
    showNotification("Connection lost, reconnecting...", "warning");
});

connection.onreconnected(() => {
    showNotification("Reconnected successfully", "success");
    rejoinConversation();
});
```

**Event handlers:**
```javascript
// Receive message from bot
connection.on("ReceiveMessage", (message) => {
    displayMessage({
        isBot: message.isBot,
        content: message.content,
        createdAt: message.createdAt
    });
    hideTypingIndicator();
});

// Receive bot response (from RabbitMQ consumer)
connection.on("BotResponse", (response) => {
    displayMessage({
        isBot: true,
        content: response.content,
        createdAt: new Date().toISOString()
    });
});
```

**Send message:**
```javascript
$('#chatForm').on('submit', async (e) => {
    e.preventDefault();
    const userInput = $('#userInput').val().trim();

    if (userInput === "") return;

    // Display user message immediately
    displayMessage({
        isBot: false,
        content: userInput,
        createdAt: new Date().toISOString()
    });

    // Show typing indicator
    showTypingIndicator();

    // Send via SignalR
    try {
        await connection.invoke("SendMessage", currentConversationId, userInput);
        $('#userInput').val('');
    } catch (err) {
        console.error(err);
        showNotification("Failed to send message", "error");
        hideTypingIndicator();
    }
});
```

**File reference:** `WebApp/wwwroot/Scripts/Chat/Chat.js`

---

## 4.5. Deployment và Infrastructure

**Nội dung chính:**

### 4.5.1. Docker Compose Configuration

**Kiến trúc deployment:** Xem **Mục 5.7** (Deployment architecture chi tiết)

**Tóm tắt docker-compose.yml:**

**13 containers:**
1. sqlserver (SQL Server 2022)
2. rabbitmq (RabbitMQ 3)
3. qdrant (Vector database)
4. ollama (LLM server)
5. minio (Object storage)
6. accountservice (.NET)
7. tenantservice (.NET)
8. documentservice (.NET)
9. storageservice (.NET)
10. chatservice (.NET)
11. embeddingservice (Python)
12. chatprocessor (Python)
13. apigateway (.NET - YARP)
14. webapp (.NET MVC) - nếu tách riêng, hoặc serve từ ApiGateway

**Network:**
```yaml
networks:
  default:
    name: aichat-network
    driver: bridge
```

**Volumes:**
```yaml
volumes:
  sqlserver-data:
  qdrant-data:
    driver_opts:
      type: none
      o: bind
      device: G:/Mount/qdrant
  ollama-data:
    driver_opts:
      device: G:/Mount/ollama
  minio-data:
    driver_opts:
      device: G:/Mount/minio
```

**Quyết định thiết kế:**

**1. Bridge network:**
- Services communicate bằng service name (e.g., `http://accountservice:8080`)
- DNS resolution tự động

**2. Persistent volumes:**
- SQL Server, Qdrant, Ollama, MinIO cần lưu trữ lâu dài
- Bind mount đến host (G:/Mount/...) để dễ backup

**Deployment Diagram:** `diagrams_to_create.md` → Diagram 4.10 (Deployment Diagram)

### 4.5.2. Environment Configuration

**appsettings.json (mỗi service):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=AIChat2025;User=sa;Password=..."
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Port": 5672
  },
  "JWT": {
    "SecretKey": "THIS_IS_A_SECRET_KEY_FOR_DEMO",  // ⚠️ Security issue
    "Issuer": "AIChat2025",
    "Audience": "AIChat2025",
    "ExpiryHours": 24
  }
}
```

**Python .env:**
```bash
OLLAMA_BASE_URL=http://ollama:11434
QDRANT_HOST=qdrant
QDRANT_PORT=6333
RABBITMQ_HOST=rabbitmq
EMBEDDING_MODEL=truro7/vn-law-embedding
LLM_MODEL=ontocord/vistral:latest
```

**Lưu ý bảo mật:** Xem `missing_implementations.md` → Security improvements

---

## 4.6. Testing Strategy (Limited)

**Nội dung chính:**

### 4.6.1. Testing Status

**Hiện trạng:**
- ❌ Unit tests: 0%
- ❌ Integration tests: 0%
- ✅ Manual testing: 100%

**Lý do:**
- Thời gian phát triển giới hạn (4 tháng)
- Ưu tiên triển khai chức năng core

**Documented as future work:** Xem `missing_implementations.md` → Section 1 (Testing)

### 4.6.2. Manual Testing Approach

**Test cases đã thực hiện:**

**1. Authentication flow:**
- Đăng ký tài khoản mới
- Đăng nhập với email/password đúng
- Đăng nhập với password sai → Error message
- Logout và verify session cleared

**2. Document upload:**
- Upload .docx file hợp lệ → Success
- Upload file không hợp lệ → Error
- Check Hangfire dashboard → Job running
- Wait for completion → Status = 'Completed'
- Verify vectors in Qdrant (Qdrant dashboard)

**3. Chat functionality:**
- Create new conversation
- Send message → Receive bot response
- Multiple messages trong conversation → Lịch sử đúng
- Refresh page → Conversation history preserved
- SignalR reconnection → Auto-reconnect successful

**4. Multi-tenant isolation:**
- Create 2 tenants
- Upload document cho tenant A
- Login as tenant B user
- Verify không thấy document của tenant A
- Chat → Chỉ tìm kiếm trong document của tenant B

**5. RAG quality:**
- Ask factual question → Correct answer with citation
- Ask ambiguous question → Reasonable response
- Ask about company rule → Correct source (company rule)
- Ask about law → Correct source (legal base)
- Ask comparison question → Both sources compared

---

## 4.7. Tổng kết chương

**Nội dung chính:**

### Những điểm chính đã trình bày:

**1. Kiến trúc hệ thống:**
- ✅ Microservices architecture (9 services)
- ✅ C4 model (Context, Container)
- ✅ Communication patterns (HTTP, RabbitMQ, SignalR)
- **Chi tiết:** Xem Mục 5.1

**2. Thiết kế database:**
- ✅ SQL Server schema (8 tables)
- ✅ Multi-tenant row-level security
- ✅ Qdrant collection schema
- **Chi tiết:** Xem Mục 5.2

**3. Triển khai microservices:**
- ✅ AccountService (authentication)
- ✅ DocumentService (document processing + Hangfire)
- ✅ ChatService (SignalR + RabbitMQ)
- ✅ EmbeddingService (text embedding + Qdrant)
- ✅ ChatProcessor (RAG pipeline 9 bước)
- **Chi tiết:** Xem Mục 5.3, 5.4, 5.5

**4. Frontend:**
- ✅ ASP.NET MVC + Razor
- ✅ SignalR client integration
- ✅ Responsive UI (Bootstrap 5)

**5. Deployment:**
- ✅ Docker Compose (13 containers)
- ✅ Bridge network
- ✅ Persistent volumes
- **Chi tiết:** Xem Mục 5.7

### Quyết định thiết kế quan trọng:

**1. Microservices vs Monolithic:**
- Chọn Microservices → Polyglot programming (.NET + Python)

**2. Multi-tenant strategy:**
- Chọn Shared Database, Row-level isolation → Chi phí thấp, đủ bảo mật

**3. RAG architecture:**
- Chọn Dual-RAG → Kết hợp company rules + legal base

**4. Message queue:**
- Chọn RabbitMQ → Decoupling, retry mechanism

**5. Real-time communication:**
- Chọn SignalR → WebSocket, auto-reconnect

### Code statistics:

**Tham khảo:** `code_statistics.json`

**Tóm tắt:**
- **Tổng files:** 188 files
- **Tổng LOC:** ~25,000 lines
- **Backend (.NET):** 144 files, 18,000 LOC
- **AI Workers (Python):** 27 files, 3,500 LOC
- **Frontend:** 17 files, 2,500 LOC
- **API endpoints:** 32 REST endpoints
- **SignalR methods:** 3 server methods, 2 client events

### Chuyển tiếp sang Chương 5:

- Chương 4 đã trình bày **thiết kế và triển khai**
- Chương 5 (đã hoàn thành) trình bày **kết quả và đánh giá**:
  - RAG evaluation metrics (RAGAS)
  - Performance benchmarks
  - Screenshots và demo
  - Phân tích ưu/nhược điểm

---

## TÀI LIỆU THAM KHẢO CHO CHƯƠNG 4

### Software Architecture
1. Richardson, C. (2018) - "Microservices Patterns"
2. Brown, S. (2021) - "The C4 Model for Visualising Software Architecture"

### Design Patterns
3. Fowler, M. (2002) - "Patterns of Enterprise Application Architecture"
4. Evans, E. (2003) - "Domain-Driven Design"

### Technologies
5. Microsoft Docs - ".NET 9 Documentation"
6. FastAPI Documentation
7. Qdrant Documentation
8. Docker Documentation

### Internal References
9. `thesis_docs/system_analysis_report.md` - Comprehensive technical documentation
10. `thesis_docs/code_statistics.json` - Code metrics
11. `thesis_docs/technology_inventory.md` - Technology reference

---

**KẾT THÚC CHƯƠNG 4**

**Điểm nhấn chính:**
- ✅ Giải thích rõ quyết định thiết kế (có alternatives, có lý do)
- ✅ Tham chiếu Chương 5 khi cần (tránh duplicate content)
- ✅ Code examples cụ thể
- ✅ Sequence diagrams, class diagrams, deployment diagrams
- ✅ Kết nối với code_statistics.json, system_analysis_report.md
- ✅ Tổng kết rõ ràng, chuyển tiếp sang Chương 5
