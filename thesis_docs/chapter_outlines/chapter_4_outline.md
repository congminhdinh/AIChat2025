# CHƯƠNG 4: THIẾT KẾ VÀ TRIỂN KHAI HỆ THỐNG

**Mục đích:** Trình bày thiết kế kiến trúc, thiết kế chi tiết, xây dựng ứng dụng, kiểm thử và triển khai hệ thống

**Số trang ước tính:** 28-32 trang

**Lưu ý quan trọng:**
- ⚠️ Chương này tập trung vào **quyết định thiết kế** và **quá trình triển khai**
- ⚠️ Các **giải pháp kỹ thuật nổi bật** được trình bày chi tiết ở Chương 5
- Tránh lặp lại nội dung Chương 5 - chỉ tóm tắt và tham chiếu

---

## 4.1. Kiến trúc hệ thống (4-5 trang)

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

**Sơ đồ chi tiết:** `diagrams_to_create.md` → Diagram 4.2 (Container Diagram)

**Tham khảo:** `chapter5guidance.txt` - Section 5.1 (System Architecture Overview)

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

**Chi tiết async processing:** Xem Mục 5.4 (Asynchronous AI Processing Pipeline)

**Checklist tài liệu cho Section 4.1:**
- [ ] System Context Diagram (C4 Level 1)
- [ ] Container Diagram (C4 Level 2)
- [ ] Communication flow diagrams
- [ ] Component descriptions

---

## 4.2. Thiết kế chi tiết (10-12 trang)

### 4.2.1. Thiết kế giao diện (2-3 trang)

**Nội dung chính:**

#### A. Frontend Architecture

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

#### B. Chat Interface Design

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
        <input type="text" id="userInput" placeholder="Hỏi về quy định công ty hoặc luật lao động..." />
        <button type="submit">Gửi</button>
    </form>
</div>
```

**3. Message rendering:**
```javascript
function displayMessage(message) {
    const messageDiv = $('<div>')
        .addClass(message.isBot ? 'message bot' : 'message user')
        .html(`
            <strong>${message.isBot ? 'Bot' : 'Bạn'}:</strong>
            <p>${escapeHtml(message.content)}</p>
            <small>${new Date(message.createdAt).toLocaleTimeString()}</small>
        `);
    $('#messages').append(messageDiv);
    scrollToBottom();
}
```

#### C. SignalR Client Integration

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
    showNotification("Mất kết nối, đang kết nối lại...", "warning");
});

connection.onreconnected(() => {
    showNotification("Đã kết nối lại thành công", "success");
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

// Send message
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

    showTypingIndicator();

    try {
        await connection.invoke("SendMessage", currentConversationId, userInput);
        $('#userInput').val('');
    } catch (err) {
        console.error(err);
        showNotification("Không thể gửi tin nhắn", "error");
        hideTypingIndicator();
    }
});
```

**File reference:** `WebApp/wwwroot/Scripts/Chat/Chat.js`

**Checklist tài liệu cho Section 4.2.1:**
- [ ] Screenshots của UI chính (Chat, Document Management, Dashboard)
- [ ] Wireframes hoặc mockups
- [ ] Responsive design examples (desktop/mobile)
- [ ] User workflow diagrams

### 4.2.2. Thiết kế lớp (3-4 trang) ⭐ NEW

**Nội dung chính:**

#### A. Design Patterns Áp Dụng

**1. Repository Pattern:**
```
Interface: IRepository<T>
Implementation: Repository<T>
Purpose: Abstraction cho data access layer
```

**2. Specification Pattern:**
```
Class: Specification<T>
Example: TenancySpecification<T>
Purpose: Encapsulate query logic, reusable queries
```

**3. Dependency Injection:**
```
Container: Microsoft.Extensions.DependencyInjection
Purpose: Loose coupling, testability
```

#### B. Class Diagram - AccountService

**Main Classes:**
```
AccountController
    ↓ depends on
AccountBusiness
    ↓ depends on
IRepository<Account>
    ↓ implements
Repository<Account>
    ↓ uses
AccountDbContext (EF Core)
```

**Key Interfaces:**
- `IRepository<T>`: Generic repository interface
- `ICurrentUserProvider`: Provides current user context (tenant_id, user_id)
- `ITokenClaimsService`: JWT token generation

**Sơ đồ chi tiết:** `diagrams_to_create.md` → Diagram 4.3 (Class Diagram - AccountService)

#### C. Class Diagram - ChatService

**Main Classes:**
```
ChatHub (SignalR Hub)
    ↓ depends on
ChatBusiness
    ↓ depends on
IRepository<ChatConversation>, IPublishEndpoint (MassTransit)
    ↓
BotResponseConsumer
    ↓ depends on
IHubContext<ChatHub>
```

**Event Classes:**
- `UserPromptReceivedEvent`: Published to RabbitMQ
- `BotResponseCreatedEvent`: Consumed from RabbitMQ

**Sơ đồ chi tiết:** `diagrams_to_create.md` → Diagram 4.4 (Class Diagram - ChatService)

#### D. Class Diagram - ChatProcessor (Python)

**Main Classes:**
```python
ChatBusiness
    ↓ depends on
QdrantService, OllamaService, RabbitMQService
    ↓
HybridSearchStrategy
    ↓ uses
LegalTermExtractor, ReciprocalRankFusion
```

**Key Classes:**
- `ChatBusiness`: Main orchestrator for RAG pipeline
- `QdrantService`: Vector search and hybrid search
- `LegalTermExtractor`: Extract Vietnamese legal terms
- `ReciprocalRankFusion`: RRF algorithm for hybrid search

**Sơ đồ chi tiết:** `diagrams_to_create.md` → Diagram 4.5 (Class Diagram - ChatProcessor)

**Checklist tài liệu cho Section 4.2.2:**
- [ ] Class diagrams cho main services (AccountService, ChatService, ChatProcessor, DocumentService)
- [ ] Interface definitions
- [ ] Relationships và dependencies
- [ ] Design patterns documentation

### 4.2.3. Thiết kế cơ sở dữ liệu (3-4 trang)

**Nội dung chính:**

#### A. Database Schema Design

**8 tables chính:**

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

**4. ChatMessages (Tin nhắn):**
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

**5. PromptDocuments (Tài liệu):**
```sql
CREATE TABLE PromptDocuments (
    Id INT PRIMARY KEY IDENTITY,
    TenantId INT NOT NULL,
    DocumentName NVARCHAR(500) NOT NULL,
    FileName NVARCHAR(500) NOT NULL,
    Type INT,  -- 1=Luật, 2=Nghị định, 3=Thông tư
    FatherDocumentId INT NULL,  -- Self-referencing FK
    Status NVARCHAR(50),  -- 'Pending', 'Processing', 'Completed', 'Failed'
    IsCompanyRule BIT DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (FatherDocumentId) REFERENCES PromptDocuments(Id)
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

**7. Permissions (Phân quyền):**
```sql
CREATE TABLE Permissions (
    Id INT PRIMARY KEY IDENTITY,
    TenantId INT NOT NULL,
    UserId INT NOT NULL,
    PermissionName NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
```

**ER Diagram:** `diagrams_to_create.md` → Diagram 4.6 (Database ER Diagram)

#### B. Multi-tenant Implementation

**Quyết định thiết kế:** Shared Database, Shared Schema với Row-level isolation

**Lý do:**
- ✅ Chi phí thấp (1 database duy nhất)
- ✅ Dễ quản lý migrations
- ✅ Đủ an toàn với automated filtering

**Implementation approach:**
- BaseEntity với TenantId column
- EF Core interceptors tự động thêm TenantId
- Specification pattern để filter theo TenantId

**Chi tiết về tenant propagation:** Xem Mục 5.3 (Infrastructure-Level Tenant Context Propagation)

**Lưu ý đặc biệt:**
- Legal base (luật nhà nước) có `TenantId = 1` (shared across all tenants)
- Company rules có `TenantId = {specific tenant}` (isolated per tenant)

#### C. Qdrant Collection Schema

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
- `text`: Indexed for full-text search (hybrid search)

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

**Chi tiết về hierarchical data model:** Xem Mục 5.1 (Hierarchical Data Modeling)

**Checklist tài liệu cho Section 4.2.3:**
- [ ] ER Diagram đầy đủ
- [ ] Table definitions với constraints
- [ ] Indexes và foreign keys
- [ ] Qdrant schema documentation

---

## 4.3. Xây dựng ứng dụng (6-8 trang)

### 4.3.1. Thư viện và công cụ sử dụng (1 trang)

**Tóm tắt technology stack:**

**Backend (.NET 9):**
- ASP.NET Core 9.0 (Web framework)
- Entity Framework Core 9.0 (ORM)
- YARP 2.3.0 (API Gateway)
- MassTransit 8.3.4 (Message bus - RabbitMQ integration)
- SignalR 1.1.0 (Real-time communication)
- Hangfire 1.8.17 (Background jobs)

**AI Workers (Python 3.11+):**
- FastAPI 0.115.0 (Web framework)
- HuggingFace Transformers (Embedding models)
- Ollama (LLM server)
- Qdrant Client (Vector DB)
- RAGAS (RAG evaluation)

**Infrastructure:**
- Docker & Docker Compose
- SQL Server 2022
- Qdrant (Vector database)
- RabbitMQ 3
- MinIO (Object storage)

**Chi tiết đầy đủ:** Xem Chương 3 (Các Công Nghệ Sử Dụng) và `technology_inventory.md`

### 4.3.2. Kết quả đạt được (1-2 trang)

**Code Statistics:**

**Tổng quan:**
- **Tổng files:** 188 files
- **Tổng LOC:** ~25,000 lines
- **Backend (.NET):** 144 files, ~18,000 LOC
- **AI Workers (Python):** 27 files, ~3,500 LOC
- **Frontend:** 17 files, ~2,500 LOC

**API Endpoints:**
- **REST API endpoints:** 32 endpoints
- **SignalR methods:** 3 server methods, 2 client events

**Database:**
- **Tables:** 8 tables (SQL Server)
- **Collections:** 1 collection (Qdrant)

**Microservices:**
- **Backend services:** 7 services (.NET)
- **AI workers:** 2 services (Python)
- **Infrastructure:** 5 containers (SQL Server, Qdrant, RabbitMQ, MinIO, Ollama)

**Tham khảo chi tiết:** `code_statistics.json`

**Development Timeline:**
- **Analysis & Design:** 3 tuần
- **Technology Selection:** 1 tuần
- **Implementation:** 10 tuần
- **Testing & Documentation:** 3 tuần
- **Total:** 17 tuần (~4 tháng)

### 4.3.3. Minh họa các chức năng chính (2-3 trang) ⭐ NEW

**Nội dung chính:**

#### A. Chức năng Đăng nhập và Xác thực

**Screenshot 1: Login Page**
- Form đăng nhập với email/password
- Remember me checkbox
- Error messages hiển thị

**Workflow:**
1. User nhập email/password
2. AccountService validates credentials
3. JWT token generated
4. Cookie stored (HttpOnly, Secure)
5. Redirect to dashboard

**File reference:** `WebApp/Views/Account/Login.cshtml`

#### B. Chức năng Chat Real-time

**Screenshot 2: Chat Interface**
- Sidebar với conversation list
- Chat area với messages (user/bot)
- Input box với send button
- Typing indicator

**Workflow:**
1. User gửi message qua SignalR
2. ChatService persists message
3. Publish `UserPromptReceivedEvent` to RabbitMQ
4. ChatProcessor consumes event
5. RAG pipeline executes (dual-RAG + hybrid search)
6. LLM generates response
7. Publish `BotResponseCreatedEvent`
8. ChatService broadcasts via SignalR
9. Frontend displays bot response

**Sequence Diagram:** `diagrams_to_create.md` → Diagram 4.7 (Chat Workflow Sequence)

#### C. Chức năng Upload và Xử lý Tài liệu

**Screenshot 3: Document Management**
- Document list với status (Pending/Processing/Completed/Failed)
- Upload button
- Document details (name, type, created date)

**Workflow:**
1. User uploads .docx file
2. DocumentService saves to MinIO
3. Create PromptDocument record (Status = 'Pending')
4. Enqueue Hangfire background job
5. Background job processes document:
   - Parse .docx
   - Hierarchical semantic chunking
   - Call EmbeddingService API
   - Store vectors in Qdrant
6. Update status to 'Completed'

**Sequence Diagram:** `diagrams_to_create.md` → Diagram 4.8 (Document Processing Sequence)

**Chi tiết về hierarchical chunking:** Xem Mục 5.1

#### D. Chức năng Dual-RAG Compliance Checking

**Screenshot 4: Compliance Check Response**
- User question: "Công ty quy định thử việc 90 ngày có hợp pháp không?"
- Bot response with 3 sections:
  1. Kết luận: KHÔNG HỢP PHÁP
  2. Quy định công ty: 90 ngày
  3. Luật nhà nước: Tối đa 60 ngày
  4. Khuyến nghị

**Chi tiết về Dual-RAG:** Xem Mục 5.2

**Checklist tài liệu cho Section 4.3.3:**
- [ ] Screenshots của 4-5 chức năng chính
- [ ] Annotated screenshots (highlight key features)
- [ ] User workflow descriptions
- [ ] Sequence diagrams

---

## 4.4. Kiểm thử (3-4 trang) ⭐ UPDATED

**Nội dung chính:**

### 4.4.1. Chiến lược kiểm thử

**Phương pháp kiểm thử:**
- **Functional Testing:** Kiểm thử chức năng hệ thống trên 5 lĩnh vực chính
- **Manual Testing:** Thực hiện manual test với 47 test cases
- **Black-box Testing:** Kiểm tra đầu ra dựa trên input, không cần biết cấu trúc nội bộ

**Scope:**
- ✅ **Functional testing:** 47 test cases across 5 domains
- ❌ **Unit testing:** 0% (documented as future work)
- ❌ **Integration testing:** 0% (documented as future work)
- ⚠️ **Performance testing:** Basic response time observation only

**Lý do thiếu unit/integration tests:**
- Thời gian phát triển giới hạn (4 tháng)
- Ưu tiên triển khai chức năng core
- Documented as future work trong `missing_implementations.md`

**Môi trường kiểm thử:**
- **Platform:** Docker Compose (13 containers)
- **LLM:** Ollama + Vistral 7B
- **Vector DB:** Qdrant
- **Database:** SQL Server 2022
- **Test data:** Vietnamese legal documents (Bộ luật Lao động 2019, company internal regulations)

### 4.4.2. Kết quả kiểm thử tổng hợp

**Bảng tổng hợp theo lĩnh vực:**

| STT | Lĩnh vực kiểm thử | Tổng số | Đạt | Không đạt | Tỷ lệ đạt |
|-----|-------------------|---------|-----|-----------|-----------|
| 1   | Quản trị          | 10      | 5   | 5         | 50.0%     |
| 2   | Lao động          | 14      | 9   | 5         | **64.3%** ⭐ |
| 3   | An sinh           | 11      | 3   | 8         | **27.3%** ❌ |
| 4   | Việc làm          | 6       | 3   | 3         | 50.0%     |
| 5   | An toàn           | 6       | 2   | 4         | 33.3%     |
| **Tổng** | **Tất cả**   | **47**  | **22** | **25** | **46.8%** |

**Nguồn dữ liệu:** `Bao_cao_kiem_thu_ngan_gon.md`

**Phân tích kết quả:**
- **Tổng số test cases:** 47
- **Số test đạt:** 22 (46.8%)
- **Số test không đạt:** 25 (53.2%)
- **Tỷ lệ Pass chung:** 46.8% (dưới 50%, cần cải thiện)

**Điểm mạnh:**
- Lĩnh vực **"Lao động"** đạt tỷ lệ cao nhất (64.3%)
- Hệ thống hoạt động tốt với các câu hỏi về luật lao động (domain chính của hệ thống)
- Dual-RAG architecture hoạt động đúng như thiết kế

**Điểm yếu:**
- Lĩnh vực **"An sinh"** đạt tỷ lệ thấp nhất (27.3%)
- Tỷ lệ pass tổng thể dưới 50% cho thấy cần cải thiện
- Retrieval effectiveness chưa đủ cao

### 4.4.3. Phân tích nguyên nhân lỗi

**Nguyên nhân chính: Hiệu quả Retrieval chưa cao**

**Root Cause Analysis:**

Tất cả 25 test cases fail đều do một nguyên nhân chính: **Semantic Gap (Khoảng cách ngữ nghĩa)**

**Mô tả vấn đề:**
- **User queries:** Ngôn ngữ thông thường, đời sống, colloquial Vietnamese
  - Ví dụ: "Nghỉ ốm có được trả lương không?"

- **Legal documents:** Thuật ngữ chuyên môn, formal, legal terminology
  - Ví dụ: "Chế độ trợ cấp ốm đau theo quy định tại Điều 138 Bộ luật Lao động 2019..."

- **Kết quả:** Vector embeddings không capture đủ tốt sự tương đồng ngữ nghĩa giữa colloquial query và formal legal text → Vector similarity thấp → Retrieval không tìm thấy đúng document

**Ví dụ cụ thể:**

| User Query (Colloquial) | Legal Document (Formal) | Vector Similarity | Result |
|------------------------|------------------------|-------------------|--------|
| "Nghỉ ốm có trả lương không?" | "Chế độ trợ cấp ốm đau Điều 138..." | 0.62 (thấp) | ❌ Không retrieve được |
| "Thử việc bao lâu?" | "Thời gian thử việc tối đa 60 ngày (Điều 24)" | 0.71 (cao hơn) | ✅ Retrieve được |
| "BHXH công ty phải đóng bao nhiêu?" | "Mức đóng bảo hiểm xã hội theo Điều 212..." | 0.68 (trung bình) | ⚠️ Không chắc chắn |

**Phân tích sâu hơn:**
1. **Abbreviation mismatch:** "BHXH" vs "Bảo hiểm xã hội" - embedding model không map tốt
2. **Synonym problem:** "Trả lương" vs "Trợ cấp" - khác nhau về semantics
3. **Question vs Statement:** User hỏi dạng câu hỏi, document là declarative statements

### 4.4.4. Giải pháp đã áp dụng (Post-Testing Improvements)

**1. Hybrid Search Implementation (Implemented 2025-12-28)**

**Vấn đề:** Vector search alone không đủ cho exact legal term matching

**Giải pháp:**
- ✅ **Legal Term Extraction:** Tự động trích xuất thuật ngữ pháp lý từ query (Điều X, BHXH, Nghị định, etc.)
- ✅ **BM25 Keyword Search:** Exact matching via Qdrant `MatchText`
- ✅ **Reciprocal Rank Fusion (RRF):** Kết hợp kết quả từ vector search + keyword search
- ✅ **Intelligent Fallback:** Tự động fallback từ tenant docs → global legal docs khi thiếu dữ liệu

**Chi tiết kỹ thuật:** Xem Mục 5.5 (Hybrid Search với RRF)

**Expected Improvement (chưa re-test):**
- Recall@5: 72% → 89% (+17% dự kiến)
- MRR: 0.68 → 0.84 (+24% dự kiến)
- Overall pass rate: 46.8% → 65%+ (dự kiến)

**2. System Instruction Enhancement**

**Giải pháp:**
- Mapping từ colloquial terms → formal legal terms
- Tenant-specific abbreviation expansion
- Query preprocessing trước khi embedding

**Ví dụ:**
```json
{
  "system_instruction": [
    {"key": "BHXH", "value": "Bảo hiểm xã hội"},
    {"key": "BHTN", "value": "Bảo hiểm thất nghiệp"},
    {"key": "nghỉ ốm", "value": "chế độ trợ cấp ốm đau"},
    {"key": "thử việc", "value": "thời gian thử việc"}
  ]
}
```

**Code reference:** `Services/ChatProcessor/src/business.py:expand_query_with_system_instruction()`

### 4.4.5. Hạn chế của chiến lược kiểm thử

**1. Thiếu Unit Tests và Integration Tests:**
- **Hiện trạng:** 0% test coverage
- **Nguyên nhân:** Thời gian phát triển giới hạn (4 tháng), ưu tiên features
- **Rủi ro:**
  - Không thể verify correctness của từng component
  - Refactoring có rủi ro cao
  - Regression bugs có thể xảy ra khi code changes
- **Documented as future work:** Xem `missing_implementations.md` - Phase 1 (4-5 tuần)

**2. Manual Testing Only:**
- **Limitations:**
  - Không scalable (phải test thủ công mỗi lần code thay đổi)
  - Không reproducible (kết quả phụ thuộc vào người test)
  - Không có automated regression testing
  - Time-consuming
- **Recommendation:** CI/CD pipeline với automated tests (future work)

**3. Không có Performance Testing đầy đủ:**
- **Hiện trạng:** Chỉ có basic observation về response time
- **Thiếu:**
  - Load testing (concurrent users)
  - Stress testing (maximum capacity)
  - Endurance testing (long-running stability)
  - Spike testing (sudden load increase)
- **Future work:** Performance testing với tools như JMeter, Locust

**4. Test Coverage không đầy đủ:**
- **Functional coverage:** 5/10+ chức năng chính (50%)
- **Edge cases:** Chưa test đầy đủ các edge cases
- **Error scenarios:** Thiếu test cho error handling, failure scenarios
- **Security testing:** Chưa có penetration testing, security audit

### 4.4.6. Kết quả quan trọng từ Testing

**Phát hiện quan trọng:**
1. **Semantic gap is the primary blocker** - 100% failures due to retrieval issues
2. **Domain-specific embeddings crucial** - Generic embeddings không đủ cho legal domain
3. **Hybrid search is necessary** - Vector-only approach has limitations
4. **System instruction is valuable** - Helps bridge colloquial ↔ formal gap

**Lessons learned:**
1. Testing should start earlier in development cycle
2. Need automated testing for continuous improvement
3. Domain knowledge is critical for RAG system design
4. Retrieval quality > LLM quality for accuracy

**Checklist tài liệu cho Section 4.4:**
- [x] Test strategy description
- [x] Test results summary table (from `Bao_cao_kiem_thu_ngan_gon.md`)
- [x] Root cause analysis
- [x] Solutions implemented (Hybrid Search)
- [x] Limitations acknowledged
- [ ] Test execution screenshots (optional)
- [ ] Test case specifications (optional - can be in appendix)

---

## 4.5. Triển khai (3-4 trang)

**Nội dung chính:**

### 4.5.1. Docker Compose Configuration

**Kiến trúc deployment:**

**13 containers:**
1. **sqlserver** (SQL Server 2022) - Relational database
2. **rabbitmq** (RabbitMQ 3) - Message broker
3. **qdrant** (Qdrant) - Vector database
4. **ollama** (Ollama) - LLM server
5. **minio** (MinIO) - Object storage
6. **accountservice** (.NET) - Authentication service
7. **tenantservice** (.NET) - Multi-tenant management
8. **documentservice** (.NET) - Document processing
9. **storageservice** (.NET) - File storage service
10. **chatservice** (.NET) - Chat orchestration
11. **embeddingservice** (Python) - Text embedding
12. **chatprocessor** (Python) - RAG pipeline
13. **apigateway** (.NET - YARP) - API Gateway + WebApp

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
- Isolated network cho security

**2. Persistent volumes:**
- SQL Server, Qdrant, Ollama, MinIO cần lưu trữ lâu dài
- Bind mount đến host (G:/Mount/...) để dễ backup
- Named volumes cho containers khác

**3. Environment variables:**
- Secrets (passwords, API keys) passed via env vars
- ⚠️ Security note: Should use secrets management in production (Azure Key Vault, HashiCorp Vault)

**Deployment Diagram:** `diagrams_to_create.md` → Diagram 4.9 (Deployment Diagram)

**Chi tiết về async processing architecture:** Xem Mục 5.4

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

**Security Notes:**
- ⚠️ **Hardcoded secrets:** JWT secret key in source code (demo only)
- ⚠️ **HTTP only:** No HTTPS in docker-compose (should use for production)
- ⚠️ **Default passwords:** SQL Server, RabbitMQ use default passwords
- **Future work:** Move secrets to Azure Key Vault, implement HTTPS

**Lưu ý bảo mật:** Xem `missing_implementations.md` → Security improvements

**Checklist tài liệu cho Section 4.5:**
- [ ] docker-compose.yml configuration
- [ ] Deployment diagram
- [ ] Environment variables documentation
- [ ] Startup instructions
- [ ] Troubleshooting guide

---

## 4.6. Tổng kết chương (1 trang)

**Nội dung chính:**

### Những điểm chính đã trình bày:

**1. Kiến trúc hệ thống (Section 4.1):**
- ✅ Microservices architecture (9 services)
- ✅ C4 model (Context, Container)
- ✅ Communication patterns (HTTP, RabbitMQ, SignalR)

**2. Thiết kế chi tiết (Section 4.2):**
- ✅ Thiết kế giao diện (ASP.NET MVC + Razor + SignalR Client)
- ✅ Thiết kế lớp (Repository pattern, Specification pattern, DI)
- ✅ Thiết kế cơ sở dữ liệu (8 tables SQL Server + Qdrant schema)

**3. Xây dựng ứng dụng (Section 4.3):**
- ✅ Thư viện và công cụ (.NET 9 + Python 3.11 + 60+ technologies)
- ✅ Kết quả đạt được (188 files, 25,000 LOC, 32 API endpoints)
- ✅ Minh họa chức năng (Chat, Document Management, Dual-RAG compliance)

**4. Kiểm thử (Section 4.4):**
- ✅ Chiến lược kiểm thử (Manual functional testing)
- ✅ Kết quả kiểm thử (47 test cases, 46.8% pass rate)
- ✅ Phân tích nguyên nhân (Semantic gap - retrieval effectiveness)
- ✅ Giải pháp (Hybrid Search implementation)
- ⚠️ Hạn chế (0% unit/integration tests - future work)

**5. Triển khai (Section 4.5):**
- ✅ Docker Compose (13 containers)
- ✅ Bridge network, persistent volumes
- ✅ Environment configuration

### Quyết định thiết kế quan trọng:

**1. Kiến trúc:**
- Chọn Microservices → Polyglot programming (.NET + Python), scalability

**2. Database:**
- Chọn Shared Database + Row-level isolation → Chi phí thấp, đủ bảo mật
- Chọn Self-referencing model → Hierarchical legal documents

**3. Communication:**
- HTTP: CRUD operations
- RabbitMQ: Long-running AI tasks
- SignalR: Real-time user experience

**4. Retrieval:**
- Dual-RAG: Company rules + Legal framework
- Hybrid Search: Vector + BM25 keyword (post-testing improvement)

**5. Deployment:**
- Docker Compose: Simple, reproducible, self-contained

### Chuyển tiếp sang Chương 5:

- Chương 4 đã trình bày **thiết kế và triển khai**
- Chương 5 sẽ trình bày **các giải pháp kỹ thuật nổi bật:**
  - 5.1. Hierarchical Data Modeling
  - 5.2. Dual-RAG Architecture
  - 5.3. Tenant Context Propagation
  - 5.4. Asynchronous AI Processing
  - 5.5. Hybrid Search với RRF

### Code statistics:

**Tham khảo:** `code_statistics.json`

**Tóm tắt:**
- **Tổng files:** 188 files
- **Tổng LOC:** ~25,000 lines
- **Backend (.NET):** 144 files, ~18,000 LOC
- **AI Workers (Python):** 27 files, ~3,500 LOC
- **Frontend:** 17 files, ~2,500 LOC
- **API endpoints:** 32 REST endpoints
- **SignalR methods:** 3 server methods, 2 client events

---

## TÀI LIỆU THAM KHẢO CHO CHƯƠNG 4

### Software Engineering
1. Ian Sommerville (2016) - "Software Engineering", 10th Edition
2. Martin Fowler (2002) - "Patterns of Enterprise Application Architecture"

### Microservices Architecture
3. Chris Richardson (2018) - "Microservices Patterns"
4. Sam Newman (2021) - "Building Microservices", 2nd Edition

### Design & Architecture
5. Simon Brown (2021) - "The C4 Model for Visualising Software Architecture"
6. Eric Evans (2003) - "Domain-Driven Design"

### Technologies
7. Microsoft Docs - ".NET 9 Documentation"
8. FastAPI Documentation
9. Qdrant Documentation
10. Docker Documentation

### Testing
11. `Bao_cao_kiem_thu_ngan_gon.md` - Test results report

### Internal References
12. `thesis_docs/system_analysis_report.md` - Technical documentation
13. `thesis_docs/code_statistics.json` - Code metrics
14. `thesis_docs/technology_inventory.md` - Technology reference
15. `thesis_docs/diagrams_to_create.md` - Diagram specifications
16. `missing_implementations.md` - Future work

---

**KẾT THÚC CHƯƠNG 4**

**Điểm nhấn chính:**
- ✅ Thiết kế rõ ràng với quyết định có lý do (alternatives, trade-offs)
- ✅ Tham chiếu Chương 5 khi cần (tránh duplicate content)
- ✅ Code examples cụ thể cho implementation details
- ✅ **Test results thực tế** từ Bao_cao_kiem_thu_ngan_gon.md
- ✅ Diagrams specifications (C4, ER, Class, Sequence, Deployment)
- ✅ Kết nối với code_statistics.json, system_analysis_report.md, technology_inventory.md
- ✅ Tổng kết rõ ràng, chuyển tiếp sang Chương 5
- ✅ Template compliance (SOICT structure matched)
- ✅ Honest acknowledgment of limitations (0% test coverage, security issues)
