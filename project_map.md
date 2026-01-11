# AIChat2025 - Project Structure Map

**Scanned at**: 2026-01-11 14:30:00
**Root Directory**: `C:\Users\MINH.DC\source\repos\AIChat2025`

---

## TONG QUAN DU AN

```
AIChat2025/
├── AdminCMS/                 (Frontend - Admin Portal)
├── ApiGateway/               (API Gateway - YARP)
├── AppHost/                  (Aspire Orchestrator)
├── Infrastructure/           (Shared Library)
├── Services/
│   ├── AccountService/       (Backend - Account Management)
│   ├── ChatProcessor/        (Python - RAG Chat Processing)
│   ├── ChatService/          (Backend - Chat/SignalR)
│   ├── DocumentService/      (Backend - Document Management)
│   ├── EmbeddingService/     (Python - Vector Embedding)
│   ├── StorageService/       (Backend - File Storage)
│   └── TenantService/        (Backend - Multi-tenant)
├── WebApp/                   (Frontend - Tenant Portal)
├── thesis_docs/              (Documentation)
├── docker-compose.yml
├── AIChat2025.sln
└── CLAUDE.md
```

---

## .NET PROJECTS

### Shared Library

#### Infrastructure
```
Path: ./Infrastructure/Infrastructure.csproj
Type: Class Library
Framework: net9.0

Key Packages:
├── Ardalis.Specification (9.3.1)
├── Ardalis.Specification.EntityFrameworkCore (9.3.1)
├── MassTransit (8.3.4)
├── MassTransit.RabbitMQ (8.3.4)
├── Microsoft.AspNetCore.Authentication.JwtBearer (9.0.8)
├── Microsoft.EntityFrameworkCore.SqlServer (9.0.11)
├── Serilog.AspNetCore (10.0.0)
└── Serilog.Enrichers.Span (3.1.0)

Purpose:
├── BaseRequest / BaseResponse wrappers
├── Entity Framework Core configurations
├── Authentication / JWT Token handling
├── Multi-tenancy support (CurrentTenantProvider)
├── Logging (Serilog)
└── Utility classes (Date, Encryption, Paging)
```

---

### Backend Microservices

#### 1. AccountService
```
Path: ./Services/AccountService/AccountService.csproj
Type: WebAPI
Framework: net9.0
Docker Port: 5051:8080

Key Packages:
├── Microsoft.EntityFrameworkCore.Design (9.0.8)
├── Microsoft.EntityFrameworkCore.Tools (9.0.8)
├── Swashbuckle.AspNetCore (6.6.2)
└── [References Infrastructure]

Connection Strings:
└── AccountDbContext → SQL Server database "AIChat2025"

Endpoints:
├── /api/auth/login
├── /api/auth/register
├── /api/account (CRUD)
└── /api/account/change-password
```

#### 2. TenantService
```
Path: ./Services/TenantService/TenantService.csproj
Type: WebAPI
Framework: net9.0
Docker Port: 5062:8080

Key Packages:
├── Microsoft.EntityFrameworkCore.Design (9.0.8)
├── Microsoft.EntityFrameworkCore.Tools (9.0.8)
├── Swashbuckle.AspNetCore (6.6.2)
└── [References Infrastructure]

Connection Strings:
└── TenantDbContext → SQL Server database "AIChat2025"

Purpose:
└── Multi-tenant management (create, read, update tenants)
```

#### 3. DocumentService
```
Path: ./Services/DocumentService/DocumentService.csproj
Type: WebAPI
Framework: net9.0
Docker Port: 5165:8080

Key Packages:
├── DocumentFormat.OpenXml (3.3.0)
├── Hangfire.Core (1.8.17)
├── Hangfire.SqlServer (1.8.17)
├── Hangfire.AspNetCore (1.8.17)
├── Qdrant.Client (1.16.1)
├── Swashbuckle.AspNetCore (6.6.2)
└── [References Infrastructure]

Connection Strings:
└── DocumentDbContext → SQL Server database "AIChat2025"

External Connections:
├── EmbeddingServiceUrl → http://localhost:8000
└── ApiGatewayUrl → http://localhost:5002

Purpose:
├── Legal document parsing (Regex patterns for Chuong, Muc, Dieu)
├── Document chunking and indexing
├── Background job processing (Hangfire)
└── Vector DB synchronization (Qdrant)
```

#### 4. StorageService
```
Path: ./Services/StorageService/StorageService.csproj
Type: WebAPI
Framework: net9.0
Docker Port: 5113:8080

Key Packages:
├── Minio (7.0.0)
├── Swashbuckle.AspNetCore (6.6.2)
└── [References Infrastructure]

External Connections:
├── MinioEndpoint → minio:9000
├── MinioBucket → ai-chat-2025
└── DocumentFilePath → /app/storages

Purpose:
└── File upload/download via MinIO Object Storage
```

#### 5. ChatService
```
Path: ./Services/ChatService/ChatService.csproj
Type: WebAPI
Framework: net9.0
Docker Port: 5218:8080

Key Packages:
├── MassTransit (8.3.4)
├── MassTransit.RabbitMQ (8.3.4)
├── Microsoft.AspNetCore.SignalR (1.1.0)
├── Swashbuckle.AspNetCore (6.9.0)
└── [References Infrastructure]

Connection Strings:
└── ChatDbContext → SQL Server database "AIChat2025"

External Connections:
├── RabbitMQEndpoint → localhost:5672
├── RabbitMQUsername → guest
└── RabbitMQPassword → guest

Purpose:
├── Real-time chat via SignalR Hub (/hubs/chat)
├── Chat conversation management
├── Message queue publishing to ChatProcessor
└── Bot response consumption (MassTransit Consumer)
```

---

### API Gateway

#### ApiGateway
```
Path: ./ApiGateway/ApiGateway.csproj
Type: YARP Reverse Proxy
Framework: net9.0
Docker Port: 5000:8080

Key Packages:
├── Yarp.ReverseProxy (2.3.0)
└── Swashbuckle.AspNetCore (6.6.2)

Routes Configuration:
├── /web-api/account/*    → http://accountservice:8080
├── /web-api/tenant/*     → http://tenantservice:8080
├── /web-api/document/*   → http://documentservice:8080
├── /web-api/storage/*    → http://storageservice:8080
├── /web-api/chat/*       → http://chatservice:8080
└── /hubs/chat/*          → http://chatservice:8080 (SignalR)
```

---

### Frontend Projects

#### 1. AdminCMS (Admin Portal)
```
Path: ./AdminCMS/AdminCMS.csproj
Type: ASP.NET MVC
Framework: net9.0

Key Packages:
└── [References Infrastructure]

Configuration:
├── WebAppUrl → https://localhost:7263
└── ApiGatewayUrl → https://localhost:7235

Purpose:
├── Admin dashboard
├── Tenant management UI
├── User/Account management UI
└── System configuration
```

#### 2. WebApp (Tenant Portal)
```
Path: ./WebApp/WebApp.csproj
Type: ASP.NET MVC
Framework: net9.0

Key Packages:
└── [References Infrastructure]

Configuration:
├── WebAppUrl → https://localhost:7262
└── ApiGatewayUrl → https://localhost:7235

Purpose:
├── Tenant user interface
├── Document upload/management
├── RAG Chat interface
└── Conversation history
```

---

### Orchestrator

#### AppHost (.NET Aspire)
```
Path: ./AppHost/AppHost.csproj
Type: Aspire AppHost
Framework: net9.0

Key Packages:
├── Aspire.AppHost.Sdk (9.3.1)
└── Aspire.Hosting.AppHost (9.3.1)

Orchestrates:
├── AdminCMS
├── ApiGateway
├── AccountService
├── ChatService
├── DocumentService
├── StorageService
├── TenantService
└── WebApp
```

---

## PYTHON SERVICES

### 1. ChatProcessor
```
Path: ./Services/ChatProcessor/
Entry: ./Services/ChatProcessor/app/main.py
       ./Services/ChatProcessor/main.py
Requirements: ./Services/ChatProcessor/requirements.txt
Docker Port: 8001:8001

Folder Structure:
ChatProcessor/
├── app/
│   ├── main.py
│   ├── models/
│   │   ├── __init__.py
│   │   └── messages.py
│   └── services/
│       ├── __init__.py
│       ├── ollama_service.py
│       ├── qdrant_service.py
│       └── rabbitmq_service.py
├── src/
│   ├── consumer.py
│   ├── evaluation_logger.py
│   ├── evaluation_service.py
│   └── logger.py
├── tests/
│   └── test_hybrid_search.py
├── docs/
├── logs/
├── .env
├── Dockerfile
├── main.py
├── requirements.txt
├── run.bat
├── run.sh
└── test_service.py

Key Dependencies:
├── fastapi==0.115.0
├── uvicorn==0.32.0
├── aio-pika==9.4.3
├── httpx==0.27.0
├── pydantic==2.9.2
├── pydantic-settings==2.5.2
├── qdrant-client==1.11.3
├── PyJWT==2.8.0
├── ragas (RAG evaluation)
└── datasets

External Connections:
├── RabbitMQ → rabbitmq:5672 (queue: chat processing)
├── Qdrant Vector DB → qdrant:6333 (hybrid search)
└── Ollama LLM → http://ollama:11434 (Qwen model)

Purpose:
├── RAG pipeline for legal document Q&A
├── Hybrid search (dense + sparse vectors)
├── LLM response generation (Ollama)
├── Message queue consumer (RabbitMQ)
└── RAG evaluation metrics (ragas)
```

### 2. EmbeddingService
```
Path: ./Services/EmbeddingService/
Entry: ./Services/EmbeddingService/main.py
Requirements: ./Services/EmbeddingService/requirements.txt
Docker Port: 8000:8000

Folder Structure:
EmbeddingService/
├── src/
│   ├── business.py
│   ├── config.py
│   ├── logger.py
│   ├── router.py
│   └── schemas.py
├── logs/
├── .env
├── Dockerfile
├── main.py
├── requirements.txt
└── run_service.bat

Key Dependencies:
├── fastapi
├── uvicorn
├── pydantic-settings
├── optimum[onnxruntime] (ONNX inference)
└── qdrant-client>=1.7.0

External Connections:
├── Qdrant Vector DB → qdrant:6333
└── QDRANT_HOST / QDRANT_PORT (env vars)

Purpose:
├── Text embedding generation (ONNX optimized)
├── Sentence transformers
├── Vector storage to Qdrant
└── FastAPI REST endpoints
```

---

## THONG KE TONG QUAN

### Projects Found
```
Total .NET Projects: 10
├── Backend Services: 5
│   ├── AccountService
│   ├── TenantService
│   ├── DocumentService
│   ├── StorageService
│   └── ChatService
├── Frontend Projects: 2
│   ├── AdminCMS
│   └── WebApp
├── API Gateway: 1
│   └── ApiGateway (YARP)
├── Shared Library: 1
│   └── Infrastructure
└── Orchestrator: 1
    └── AppHost (Aspire)

Total Python Services: 2
├── ChatProcessor (RAG Pipeline)
└── EmbeddingService (Vector Generation)
```

### Technology Stack
```
Backend:
├── .NET 9 (C# 12)
├── Entity Framework Core 9.0
├── YARP 2.3 (API Gateway)
├── MassTransit 8.3 (Message Bus)
├── SignalR (Real-time)
└── Ardalis.Specification (Repository Pattern)

AI Layer:
├── Python 3.11+
├── FastAPI 0.115
├── ONNX Runtime (Optimum)
├── Qdrant Client 1.11+
└── Ragas (RAG Evaluation)

Data & Infrastructure:
├── SQL Server 2022 (Relational data)
├── Qdrant (Vector database)
├── MinIO (Object storage)
├── Ollama (LLM runtime)
└── RabbitMQ (Message queue)

DevOps:
├── Docker Compose
├── .NET Aspire 9.3
└── Serilog (Logging)
```

---

## VERIFICATION CHECKLIST

Kiem tra cac yeu to sau da duoc tim thay:

- [x] AccountService (Backend)
- [x] TenantService (Backend)
- [x] ChatService (Backend)
- [x] DocumentService (Backend)
- [x] StorageService (Backend)
- [x] Gateway (YARP - ApiGateway)
- [x] WebApp (Frontend)
- [x] AdminCMS (Frontend)
- [x] ChatProcessor (Python)
- [x] EmbeddingService (Python)
- [x] Infrastructure (Shared Library)
- [x] AppHost (Aspire Orchestrator)

---

## NOTES

### Cau truc du an
1. **Monorepo Architecture**: Tat ca services nam trong mot solution (AIChat2025.sln)
2. **Aspire Integration**: Su dung .NET Aspire de orchestrate local development
3. **Shared Infrastructure**: Infrastructure project chua code dung chung (BaseResponse, BaseRequest, Tenancy, Authentication)

### Multi-tenancy
- `TenantHashConstant` trong Infrastructure cho tenant isolation
- `CurrentTenantProvider` inject tenant context
- `TenancySpecification` filter data theo tenant

### Communication Patterns
```
Frontend Apps ─────► ApiGateway ─────► Backend Services
                          │
                          ├──► AccountService
                          ├──► TenantService
                          ├──► DocumentService
                          ├──► StorageService
                          └──► ChatService ─────► RabbitMQ ─────► ChatProcessor
                                    │                                   │
                                    │                                   ▼
                                    └──────────────────────────────── Ollama LLM
                                                                        │
                                                                        ▼
                                                               Qdrant (Vector Search)
```

### Database Schema (Shared)
- Tat ca services dung chung database `AIChat2025` tren SQL Server
- Moi service co DbContext rieng (AccountDbContext, TenantDbContext, etc.)

### Port Mapping (Docker)
```
Service             │ Host Port │ Container Port
────────────────────┼───────────┼────────────────
SQL Server          │ 1433      │ 1433
RabbitMQ            │ 5672      │ 5672
RabbitMQ Management │ 15672     │ 15672
Qdrant              │ 6333      │ 6333
Ollama              │ 11434     │ 11434
MinIO               │ 9000/9001 │ 9000/9001
AccountService      │ 5051      │ 8080
TenantService       │ 5062      │ 8080
DocumentService     │ 5165      │ 8080
StorageService      │ 5113      │ 8080
ChatService         │ 5218      │ 8080
ApiGateway          │ 5000      │ 8080
EmbeddingService    │ 8000      │ 8000
ChatProcessor       │ 8001      │ 8001
```

### De xuat cai tien
1. **Separate Database per Service**: Hien tai tat ca services dung chung 1 database, nen tach ra de dam bao microservice independence
2. **Health Checks**: Them health check endpoints cho moi service
3. **Service Discovery**: Xem xet su dung Consul/Eureka thay vi hardcode URLs
4. **Centralized Configuration**: Xem xet su dung Azure App Configuration hoac HashiCorp Vault

---

*Generated by Claude Code - Phase 1 Discovery*
