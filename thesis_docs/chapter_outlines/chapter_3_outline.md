# CHƯƠNG 3: CÁC CÔNG NGHỆ SỬ DỤNG

**Mục đích:** Giới thiệu chi tiết các công nghệ, thư viện, framework được sử dụng trong hệ thống

**Số trang ước tính:** 12-15 trang

**Lưu ý:** Tham khảo `thesis_docs/technology_inventory.md` cho danh sách đầy đủ 60+ công nghệ

---

## 3.1. Tổng quan về stack công nghệ

**Nội dung chính:**

### 3.1.1. Technology Stack Overview

**Kiến trúc 4 tầng:**
```
┌─────────────────────────────────────────┐
│         FRONTEND (Presentation)         │
│  ASP.NET MVC 9 + Razor + Bootstrap 5    │
│  SignalR Client + jQuery                │
└─────────────────────────────────────────┘
                   ↓ HTTPS/WebSocket
┌─────────────────────────────────────────┐
│      BACKEND SERVICES (Application)     │
│  .NET 9 Microservices (7 services)      │
│  API Gateway (YARP)                     │
└─────────────────────────────────────────┘
                   ↓ RabbitMQ/HTTP
┌─────────────────────────────────────────┐
│       AI WORKERS (Intelligence)         │
│  Python FastAPI (2 workers)             │
│  HuggingFace + Ollama + RAGAS           │
└─────────────────────────────────────────┘
                   ↓ SQL/Vector/S3
┌─────────────────────────────────────────┐
│      INFRASTRUCTURE (Data & Infra)      │
│  SQL Server + Qdrant + MinIO + RabbitMQ │
│  Docker Compose (13 containers)         │
└─────────────────────────────────────────┘
```

**Sơ đồ:** `diagrams_to_create.md` → Diagram 3.1 (Technology Stack Overview)

### 3.1.2. Lý do lựa chọn stack này

**1. .NET 9 cho backend:**
- ✅ Hiệu năng cao (faster than Node.js, comparable to Go)
- ✅ Type-safe (C#), giảm lỗi runtime
- ✅ Entity Framework Core 9 (ORM mạnh mẽ)
- ✅ Microservices-friendly (minimal APIs, gRPC support)
- ✅ Cross-platform (Windows, Linux, macOS)

**2. Python cho AI workers:**
- ✅ Ecosystem AI/ML phong phú nhất (HuggingFace, PyTorch, Scikit-learn)
- ✅ FastAPI (async, hiệu năng cao, auto OpenAPI docs)
- ✅ Dễ tích hợp với Ollama, RAGAS

**3. Polyglot architecture (.NET + Python):**
- ✅ Best of both worlds: .NET cho business logic, Python cho AI
- ✅ Giao tiếp qua RabbitMQ (language-agnostic)

**4. Docker Compose:**
- ✅ Đơn giản deploy local (1 lệnh: docker-compose up)
- ✅ Reproducible environment
- ✅ Không cần Kubernetes (quá phức tạp cho thesis)

---

## 3.2. Backend Framework - .NET 9

**Nội dung chính:**

### 3.2.1. .NET 9 và ASP.NET Core

**Giới thiệu:**
- .NET 9: Phiên bản mới nhất (November 2024)
- ASP.NET Core 9: Framework web của .NET
- Minimal APIs: Cú pháp đơn giản, không cần controllers

**Ví dụ Minimal API:**
```csharp
var app = WebApplication.Create(args);

app.MapGet("/api/tenants", async (IRepository<Tenant> repo) =>
{
    var tenants = await repo.ListAsync();
    return Results.Ok(tenants);
});

app.Run();
```

**Ưu điểm:**
- Ít boilerplate code hơn MVC controllers
- Hiệu năng cao hơn
- Phù hợp microservices

### 3.2.2. Entity Framework Core 9

**Giới thiệu:**
- ORM (Object-Relational Mapping) của .NET
- Ánh xạ C# objects ↔ SQL tables
- Code-first approach

**Ví dụ:**
```csharp
// Entity
public class Account : BaseEntity
{
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public int TenantId { get; set; }
}

// DbContext
public class AccountDbContext : BaseDbContext
{
    public DbSet<Account> Accounts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Account>()
            .HasIndex(a => new { a.TenantId, a.Email })
            .IsUnique();
    }
}

// Query
var account = await _dbContext.Accounts
    .Where(a => a.Email == email && a.TenantId == tenantId)
    .FirstOrDefaultAsync();
```

**Tính năng sử dụng:**
- Migrations: Quản lý schema changes
- Interceptors: Tự động thêm TenantId, UpdatedAt (xem Mục 5.2)
- Lazy loading: Load related entities on-demand
- Query tracking: Optimize read performance

### 3.2.3. YARP - API Gateway

**Giới thiệu:**
- YARP (Yet Another Reverse Proxy) - Microsoft's reverse proxy
- Sử dụng làm API Gateway

**Chức năng:**
- Routing: /api/account/* → AccountService
- Load balancing: Phân tải giữa nhiều instance (nếu có)
- Authentication forwarding: Forward JWT token
- Swagger aggregation: Tổng hợp API docs từ nhiều services

**Cấu hình:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "account-route": {
        "ClusterId": "account-cluster",
        "Match": {
          "Path": "/api/account/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "account-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://accountservice:8080"
          }
        }
      }
    }
  }
}
```

**Tham khảo:** Mục 5.1.3 (API Gateway architecture)

### 3.2.4. MassTransit - Message Bus

**Giới thiệu:**
- Abstraction layer cho RabbitMQ, Azure Service Bus, Amazon SQS
- Hỗ trợ publish/subscribe, request/response patterns

**Ví dụ:**
```csharp
// Publisher (ChatService)
await _publishEndpoint.Publish(new UserPromptReceivedEvent
{
    ConversationId = conversationId,
    Message = userMessage,
    TenantId = tenantId,
    UserId = userId
});

// Consumer (ChatProcessor - Python sẽ consume từ RabbitMQ trực tiếp)
```

**Ưu điểm:**
- Retry mechanism tự động
- Error handling
- Message serialization (JSON)

### 3.2.5. SignalR - Real-time Communication

**Giới thiệu:**
- WebSocket library của ASP.NET Core
- Hỗ trợ real-time bidirectional communication

**Ví dụ:**
```csharp
// Server (ChatHub)
public class ChatHub : Hub
{
    public async Task SendMessage(string conversationId, string message)
    {
        // Process message...
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", response);
    }

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }
}

// Client (JavaScript)
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

connection.on("ReceiveMessage", (message) => {
    displayMessage(message);
});

await connection.invoke("SendMessage", conversationId, userInput);
```

**Use case trong AIChat2025:**
- Gửi message từ client → server
- Nhận bot response real-time
- Hiển thị "typing..." indicator

**Tham khảo:** Mục 5.4 (Real-time communication architecture)

### 3.2.6. Hangfire - Background Jobs

**Giới thiệu:**
- Thư viện cho background job processing
- Persistent job storage (SQL Server)
- Web dashboard

**Ví dụ:**
```csharp
// Enqueue job
BackgroundJob.Enqueue<VectorizeBackgroundJob>(job =>
    job.VectorizeDocumentAsync(documentId));

// Job implementation
public class VectorizeBackgroundJob
{
    public async Task VectorizeDocumentAsync(int documentId)
    {
        // 1. Retrieve document from database
        // 2. Parse .docx file
        // 3. Hierarchical chunking
        // 4. Call EmbeddingService API
        // 5. Store vectors in Qdrant
    }
}
```

**Use case:**
- Document vectorization (tốn thời gian, không thể làm synchronous)
- Không block user request

**Dashboard:**
- Xem jobs đang chạy, completed, failed
- Retry failed jobs

**Tham khảo:** Mục 5.3.3 (Document vectorization flow)

### 3.2.7. Serilog - Structured Logging

**Giới thiệu:**
- Thư viện logging cho .NET
- Structured logs (JSON format)

**Ví dụ:**
```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", "AIChat2025")
    .Enrich.WithSpan() // Distributed tracing
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

_logger.LogInformation("User {UserId} sent message to conversation {ConversationId}",
    userId, conversationId);
```

**Output:**
```json
{
  "Timestamp": "2024-12-28T10:30:45.123",
  "Level": "Information",
  "MessageTemplate": "User {UserId} sent message...",
  "Properties": {
    "UserId": 123,
    "ConversationId": 456,
    "Application": "AIChat2025"
  }
}
```

### 3.2.8. Ardalis.Specification - Repository Pattern

**Giới thiệu:**
- Thư viện hỗ trợ Specification pattern
- Encapsulate query logic

**Ví dụ:**
```csharp
// Specification
public class TenancySpecification<T> : Specification<T> where T : BaseEntity
{
    public TenancySpecification(int tenantId)
    {
        Query.Where(e => e.TenantId == tenantId);
    }
}

// Usage
var spec = new TenancySpecification<Account>(tenantId);
var accounts = await _repository.ListAsync(spec);
```

**Ưu điểm:**
- Reusable query logic
- Testable (mock specification)

---

## 3.3. AI Worker Framework - Python

**Nội dung chính:**

### 3.3.1. FastAPI

**Giới thiệu:**
- Modern Python web framework
- Async support (asyncio)
- Auto OpenAPI documentation
- Type hints với Pydantic

**Ví dụ:**
```python
from fastapi import FastAPI
from pydantic import BaseModel

app = FastAPI()

class EmbedRequest(BaseModel):
    text: str

@app.post("/embed")
async def embed_text(request: EmbedRequest):
    vector = embedding_service.encode_text(request.text)
    return {"vector": vector.tolist(), "dimension": len(vector)}
```

**Ưu điểm:**
- Hiệu năng cao (so với Flask)
- Validation tự động (Pydantic)
- Async/await native support

### 3.3.2. Uvicorn - ASGI Server

**Giới thiệu:**
- ASGI (Asynchronous Server Gateway Interface) server
- Chạy FastAPI application

**Usage:**
```bash
uvicorn main:app --host 0.0.0.0 --port 8000
```

### 3.3.3. Pydantic - Data Validation

**Giới thiệu:**
- Data validation library
- Type hints enforcement

**Ví dụ:**
```python
from pydantic import BaseModel, Field, field_validator

class ChatRequest(BaseModel):
    conversation_id: int = Field(gt=0)
    message: str = Field(min_length=1, max_length=1000)
    tenant_id: int

    @field_validator('message')
    def validate_message(cls, v):
        if v.strip() == "":
            raise ValueError("Message cannot be empty")
        return v
```

**Ưu điểm:**
- Validation tự động
- Error messages rõ ràng
- IDE autocomplete support

---

## 3.4. AI & Machine Learning Libraries

**Nội dung chính:**

### 3.4.1. HuggingFace Transformers

**Giới thiệu:**
- Thư viện để load pre-trained transformer models
- Hỗ trợ hàng ngàn models từ HuggingFace Hub

**Usage trong EmbeddingService:**
```python
from sentence_transformers import SentenceTransformer

model = SentenceTransformer('truro7/vn-law-embedding')

# Encode text to vector
text = "Thời gian thử việc là bao lâu?"
vector = model.encode(text)  # Output: numpy array (768 dimensions)
```

**Model sử dụng:**
- `truro7/vn-law-embedding`: 768-dim, specialized cho pháp luật Việt Nam
- Fine-tuned trên legal corpus

### 3.4.2. ONNX Runtime (via Optimum)

**Giới thiệu:**
- Tối ưu hóa inference speed
- Convert PyTorch model → ONNX format

**Ưu điểm:**
- Nhanh hơn PyTorch inference (2-3x)
- Ít RAM hơn
- CPU-friendly

**Usage:**
```python
from optimum.onnxruntime import ORTModelForFeatureExtraction

model = ORTModelForFeatureExtraction.from_pretrained(
    'truro7/vn-law-embedding',
    export=True  # Auto convert to ONNX
)
```

### 3.4.3. Ollama - Local LLM Server

**Giới thiệu:**
- Chạy LLM locally (không cần API key)
- Hỗ trợ nhiều models: LLaMA, Mistral, Gemma, Vistral
- REST API

**Cài đặt:**
```bash
# Pull model
ollama pull ontocord/vistral:latest

# Run server (port 11434)
ollama serve
```

**Usage trong ChatProcessor:**
```python
import httpx

async def generate_response(prompt: str) -> str:
    async with httpx.AsyncClient() as client:
        response = await client.post(
            "http://ollama:11434/api/generate",
            json={
                "model": "ontocord/vistral:latest",
                "prompt": prompt,
                "stream": False
            }
        )
        return response.json()["response"]
```

**Model sử dụng:**
- `ontocord/vistral:latest`: Vietnamese-finetuned LLaMA
- 7B parameters
- Context length: 4096 tokens

### 3.4.4. RAGAS - RAG Evaluation Framework

**Giới thiệu:**
- Framework đánh giá chất lượng RAG system
- Metrics: Faithfulness, Answer Relevancy, Context Recall, Context Precision

**Metrics:**

**1. Faithfulness (Độ trung thực):**
```
Câu trả lời có trung thực với context không?
= (Số claims được hỗ trợ bởi context) / (Tổng số claims)

Ví dụ:
Context: "Thời gian thử việc tối đa 60 ngày (Điều 24)"
Answer: "Thử việc tối đa 60 ngày theo Điều 24"
→ Faithfulness = 1.0 (100% trung thực)

Bad Answer: "Thử việc tối đa 90 ngày"
→ Faithfulness = 0.0 (sai thông tin)
```

**2. Answer Relevancy (Độ liên quan của câu trả lời):**
```
Câu trả lời có liên quan đến câu hỏi không?

Question: "Thời gian thử việc là bao lâu?"
Answer: "Tối đa 60 ngày"
→ Answer Relevancy = 1.0 (rất liên quan)

Bad Answer: "Có nhiều quy định về lao động"
→ Answer Relevancy = 0.3 (không trả lời câu hỏi)
```

**3. Context Recall (Độ bao phủ context):**
```
Hệ thống có tìm được tất cả context liên quan không?
= (Số ground truth statements xuất hiện trong context) / (Tổng ground truth statements)

Ground Truth: "Điều 24: Thử việc tối đa 60 ngày"
Retrieved Context: ["Điều 24...", "Điều 25..."]
→ Context Recall = 1.0 (đã tìm được)

Bad: Không tìm được Điều 24
→ Context Recall = 0.0
```

**4. Context Precision (Độ chính xác context):**
```
Context có chứa nhiều thông tin nhiễu không?
= Weighted precision based on relevant chunks

Retrieved: [Chunk 1 (relevant), Chunk 2 (relevant), Chunk 3 (irrelevant), ...]
→ Context Precision = (1 + 1/2 + 0) / 3 = 0.5
```

**Usage:**
```python
from ragas import evaluate
from ragas.metrics import faithfulness, answer_relevancy, context_recall, context_precision

result = evaluate(
    dataset,
    metrics=[faithfulness, answer_relevancy, context_recall, context_precision]
)

print(result)
# Output:
# {
#   "faithfulness": 0.92,
#   "answer_relevancy": 0.88,
#   "context_recall": 0.85,
#   "context_precision": 0.78
# }
```

**Tham khảo:** Chương 5.2 (RAG Evaluation Results)

---

## 3.5. Databases

**Nội dung chính:**

### 3.5.1. SQL Server 2022

**Giới thiệu:**
- Relational database của Microsoft
- Sử dụng Developer Edition (miễn phí)

**Tính năng sử dụng:**
- Tables: Accounts, Tenants, Permissions, ChatConversations, ChatMessages, PromptConfigs, PromptDocuments
- Indexes: Primary keys, foreign keys, composite indexes
- Constraints: Unique, NOT NULL, CHECK
- Transactions: ACID compliance

**Connection String:**
```
Server=sqlserver;Database=AIChat2025;User=sa;Password=YourStrong@Passw0rd;
TrustServerCertificate=True;MultipleActiveResultSets=True;
```

**Tham khảo:** Mục 5.2.2 (Database schema design)

### 3.5.2. Qdrant - Vector Database

**Giới thiệu:**
- Vector database open-source
- Written in Rust (hiệu năng cao)
- REST API + gRPC

**Concepts:**

**1. Collection:**
```
Collection = Một bộ vectors cùng schema
Ví dụ: "vn_law_documents"
```

**2. Point:**
```
Point = 1 vector + metadata (payload)
{
  "id": "uuid-123",
  "vector": [0.23, -0.45, ..., 0.12],  // 768 dimensions
  "payload": {
    "text": "Thời gian thử việc tối đa 60 ngày...",
    "document_name": "Bộ luật Lao động 2019",
    "heading1": "Chương XV",
    "heading2": "Điều 24",
    "tenant_id": 1
  }
}
```

**3. Search:**
```python
from qdrant_client import QdrantClient

client = QdrantClient(host="qdrant", port=6333)

# Vector search
results = client.search(
    collection_name="vn_law_documents",
    query_vector=query_embedding,  # 768-dim vector
    limit=5,
    query_filter={
        "must": [
            {"key": "tenant_id", "match": {"value": tenant_id}}
        ]
    }
)

# Results sorted by similarity (COSINE distance)
for result in results:
    print(f"Score: {result.score}")
    print(f"Text: {result.payload['text']}")
    print(f"Source: {result.payload['document_name']}")
```

**Distance Metric:**
- COSINE similarity: Đo góc giữa 2 vectors
- Range: [-1, 1], higher = more similar

**Indexing:**
- HNSW (Hierarchical Navigable Small World): Graph-based index
- Fast approximate nearest neighbor search
- Trade-off: Precision vs Speed

**Tham khảo:** Mục 5.4.2 (Vector search implementation)

---

## 3.6. Message Queue & Storage

**Nội dung chính:**

### 3.6.1. RabbitMQ

**Giới thiệu:**
- Message broker (AMQP protocol)
- Hỗ trợ nhiều messaging patterns

**Concepts:**

**1. Exchange:**
```
Exchange = Router, quyết định message đi đâu
Types: Direct, Fanout, Topic, Headers
```

**2. Queue:**
```
Queue = Buffer lưu messages
Consumers đọc từ queue
```

**3. Binding:**
```
Binding = Kết nối giữa exchange và queue
```

**Flow trong AIChat2025:**
```
ChatService (Publisher)
    ↓ Publish event "UserPromptReceived"
Exchange (Fanout)
    ↓ Route to queue
Queue: "UserPromptReceived"
    ↓ Consume
ChatProcessor (Consumer - Python)
```

**Python Client:**
```python
import aio_pika

connection = await aio_pika.connect_robust("amqp://rabbitmq:5672")
channel = await connection.channel()

# Declare queue
queue = await channel.declare_queue("UserPromptReceived", durable=True)

# Consume messages
async with queue.iterator() as queue_iter:
    async for message in queue_iter:
        async with message.process():
            data = json.loads(message.body)
            await process_prompt(data)
```

**Management UI:**
- Port 15672
- Xem queues, exchanges, message rates

### 3.6.2. MinIO - Object Storage

**Giới thiệu:**
- S3-compatible object storage
- Open-source alternative to AWS S3

**Concepts:**

**1. Bucket:**
```
Bucket = Namespace cho objects
Ví dụ: "ai-chat-2025"
```

**2. Object:**
```
Object = File + metadata
Path: ai-chat-2025/tenant_1/document_123.docx
```

**.NET Client:**
```csharp
using Minio;

var minio = new MinioClient()
    .WithEndpoint("minio:9000")
    .WithCredentials("minioadmin", "minioadmin")
    .Build();

// Upload
await minio.PutObjectAsync(new PutObjectArgs()
    .WithBucket("ai-chat-2025")
    .WithObject($"tenant_{tenantId}/{fileName}")
    .WithStreamData(fileStream)
    .WithObjectSize(fileStream.Length)
    .WithContentType(contentType));

// Download
await minio.GetObjectAsync(new GetObjectArgs()
    .WithBucket("ai-chat-2025")
    .WithObject($"tenant_{tenantId}/{fileName}")
    .WithCallbackStream(stream => stream.CopyTo(outputStream)));
```

**Use case:**
- Lưu trữ file .docx upload bởi users
- Cheaper than database BLOB storage

---

## 3.7. Containerization & Orchestration

**Nội dung chính:**

### 3.7.1. Docker

**Giới thiệu:**
- Container platform
- Đóng gói application + dependencies vào image

**Dockerfile ví dụ (AccountService):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Services/AccountService/AccountService.csproj", "Services/AccountService/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "Services/AccountService/AccountService.csproj"
COPY . .
RUN dotnet build "Services/AccountService/AccountService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/AccountService/AccountService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccountService.dll"]
```

**Ưu điểm:**
- Consistent environment (dev = prod)
- Easy deployment (docker run)
- Resource isolation

### 3.7.2. Docker Compose

**Giới thiệu:**
- Orchestration tool cho multi-container applications
- YAML configuration

**docker-compose.yml (simplified):**
```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong@Passw0rd"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"

  qdrant:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333"
    volumes:
      - G:/Mount/qdrant:/qdrant/storage

  accountservice:
    build:
      context: .
      dockerfile: Services/AccountService/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=AIChat2025;..."
      RabbitMQ__Host: "rabbitmq"
    depends_on:
      - sqlserver
      - rabbitmq
    ports:
      - "5050:8080"

  # ... 12 more services ...

networks:
  default:
    name: aichat-network

volumes:
  sqlserver-data:
```

**Commands:**
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

**Sơ đồ:** `diagrams_to_create.md` → Diagram 3.2 (Docker Compose Architecture)

---

## 3.8. Frontend Technologies

**Nội dung chính:**

### 3.8.1. ASP.NET Core MVC + Razor

**Giới thiệu:**
- Server-side rendering
- Razor syntax: HTML + C#

**Ví dụ:**
```cshtml
@model ChatViewModel

<div class="container">
    <h1>Conversation: @Model.ConversationTitle</h1>

    <div id="messages">
        @foreach (var msg in Model.Messages)
        {
            <div class="message @(msg.IsBot ? "bot" : "user")">
                <strong>@(msg.IsBot ? "Bot" : "You"):</strong>
                <p>@msg.Content</p>
            </div>
        }
    </div>

    <form id="chatForm">
        <input type="text" id="userInput" placeholder="Type your question..." />
        <button type="submit">Send</button>
    </form>
</div>

@section Scripts {
    <script src="~/Scripts/Chat/Chat.js"></script>
}
```

### 3.8.2. Bootstrap 5

**Giới thiệu:**
- CSS framework
- Responsive design (mobile-first)

**Usage:**
```html
<div class="container">
    <div class="row">
        <div class="col-md-8 offset-md-2">
            <div class="card">
                <div class="card-body">
                    <!-- Content -->
                </div>
            </div>
        </div>
    </div>
</div>
```

### 3.8.3. jQuery & SignalR Client

**jQuery:**
- DOM manipulation
- AJAX calls
- Event handling

**SignalR JavaScript Client:**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

connection.on("ReceiveMessage", (message) => {
    displayMessage(message);
});

await connection.start();
await connection.invoke("SendMessage", conversationId, userInput);
```

---

## 3.9. Bảng tổng hợp công nghệ

**Tham khảo:** `thesis_docs/technology_inventory.md`

**Tóm tắt:**

| Layer | Technology | Version | License | Purpose |
|-------|-----------|---------|---------|---------|
| **Backend** | .NET 9 | 9.0 | MIT | Backend microservices |
| | Entity Framework Core | 9.0.11 | MIT | ORM |
| | YARP | 2.3.0 | MIT | API Gateway |
| | MassTransit | 8.3.4 | Apache 2.0 | Message bus |
| | SignalR | 1.1.0 | MIT | Real-time |
| | Hangfire | 1.8.17 | LGPL-3.0 | Background jobs |
| **AI Workers** | Python | 3.11+ | PSF | AI runtime |
| | FastAPI | 0.115.0 | MIT | Web framework |
| | HuggingFace Transformers | Latest | Apache 2.0 | ML models |
| | Ollama | Latest | MIT | LLM server |
| | RAGAS | Latest | Apache 2.0 | RAG evaluation |
| **Databases** | SQL Server | 2022 | Proprietary (free dev) | Relational DB |
| | Qdrant | Latest | Apache 2.0 | Vector DB |
| **Infrastructure** | RabbitMQ | 3 | MPL 2.0 | Message queue |
| | MinIO | Latest | AGPL-3.0 | Object storage |
| | Docker | Latest | Apache 2.0 | Containerization |
| **Frontend** | ASP.NET MVC | 9.0 | MIT | Web framework |
| | Bootstrap | 5.x | MIT | CSS framework |
| | jQuery | 3.x | MIT | JavaScript |
| | SignalR Client | 8.0.0 | MIT | WebSocket client |

**Tổng cộng:** 60+ thư viện và công nghệ

---

## 3.10. Tổng kết chương

**Nội dung chính:**

### Những điểm chính đã trình bày:
1. ✅ Tổng quan technology stack 4 tầng
2. ✅ Chi tiết .NET 9 ecosystem (EF Core, YARP, MassTransit, SignalR, Hangfire)
3. ✅ Chi tiết Python ecosystem (FastAPI, HuggingFace, Ollama, RAGAS)
4. ✅ Database technologies (SQL Server, Qdrant)
5. ✅ Infrastructure (Docker, RabbitMQ, MinIO)
6. ✅ Frontend technologies (ASP.NET MVC, Bootstrap, jQuery)

### Lý do lựa chọn các công nghệ:
- .NET 9: Hiệu năng, type-safe, microservices-friendly
- Python: AI/ML ecosystem phong phú
- Qdrant: Vector DB nhanh, open-source
- RabbitMQ: Message queue ổn định
- Docker Compose: Đơn giản deploy local

### Chuyển tiếp sang Chương 4:
- Chương 3 đã giới thiệu **công nghệ sử dụng**
- Chương 4 sẽ trình bày **cách thiết kế và triển khai** hệ thống sử dụng các công nghệ này

**Lưu ý:** Đối với các chi tiết triển khai (kiến trúc, database schema, RAG pipeline), xem Mục 5 (đã hoàn thành) hoặc Chương 4 (sẽ tạo tiếp).

---

**KẾT THÚC CHƯƠNG 3**

**Điểm nhấn chính:**
- ✅ Giới thiệu đầy đủ 60+ công nghệ
- ✅ Giải thích lý do lựa chọn (có so sánh)
- ✅ Code examples cụ thể cho mỗi công nghệ
- ✅ Tham chiếu đến technology_inventory.md
- ✅ Liên kết với Chương 4 (triển khai) và Chương 5 (kết quả)
