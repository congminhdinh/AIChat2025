# Technology Inventory - AIChat2025

**Generated:** 2025-12-28
**Project:** Multi-tenant RAG Legal Chat System
**Purpose:** Complete list of technologies and libraries for graduation thesis documentation

---

## COMPREHENSIVE TECHNOLOGY TABLE

| Purpose | Technology/Library | Version | License | URL | Usage in Project |
|---------|-------------------|---------|---------|-----|------------------|
| **BACKEND FRAMEWORK** |
| Web Framework | ASP.NET Core | 9.0 | MIT | https://dotnet.microsoft.com/ | All backend microservices |
| ORM | Entity Framework Core | 9.0.11 | MIT | https://docs.microsoft.com/ef/ | Database access across all .NET services |
| API Gateway | YARP (Yet Another Reverse Proxy) | 2.3.0 | MIT | https://microsoft.github.io/reverse-proxy/ | ApiGateway routing and load balancing |
| Message Bus | MassTransit | 8.3.4 | Apache 2.0 | https://masstransit-project.com/ | RabbitMQ integration in all services |
| RabbitMQ Client | MassTransit.RabbitMQ | 8.3.4 | Apache 2.0 | https://masstransit-project.com/ | AMQP messaging implementation |
| Real-Time Communication | SignalR | 1.1.0 | MIT | https://dotnet.microsoft.com/apps/aspnet/signalr | ChatService WebSocket hub |
| Background Jobs | Hangfire.Core | 1.8.17 | LGPL-3.0 | https://www.hangfire.io/ | DocumentService background vectorization |
| Hangfire Storage | Hangfire.SqlServer | 1.8.17 | LGPL-3.0 | https://www.hangfire.io/ | Job persistence in SQL Server |
| Hangfire Web | Hangfire.AspNetCore | 1.8.17 | LGPL-3.0 | https://www.hangfire.io/ | Job dashboard UI |
| Repository Pattern | Ardalis.Specification | 9.3.1 | MIT | https://github.com/ardalis/specification | Query encapsulation pattern |
| EF Specification | Ardalis.Specification.EntityFrameworkCore | 9.3.1 | MIT | https://github.com/ardalis/specification | EF Core integration |
| Document Processing | DocumentFormat.OpenXml | 3.3.0 | MIT | https://github.com/OfficeDev/Open-XML-SDK | .docx file parsing in DocumentService |
| Object Storage Client | Minio | 7.0.0 | Apache 2.0 | https://min.io/ | S3-compatible file storage |
| Vector DB Client | Qdrant.Client | 1.16.1 | Apache 2.0 | https://qdrant.tech/ | Vector database integration |
| Logging | Serilog.AspNetCore | 10.0.0 | Apache 2.0 | https://serilog.net/ | Structured logging |
| Logging Enrichment | Serilog.Enrichers.Span | 3.1.0 | Apache 2.0 | https://github.com/serilog/serilog-enrichers-span | Distributed tracing |
| **DATABASE** |
| SQL Database | SQL Server | 2022 | Proprietary (Dev free) | https://www.microsoft.com/sql-server | Relational data storage |
| EF Provider | Microsoft.EntityFrameworkCore.SqlServer | 9.0.11 | MIT | https://docs.microsoft.com/ef/ | SQL Server provider |
| EF Provider (Dev) | Microsoft.EntityFrameworkCore.Sqlite | 9.0.8 | MIT | https://docs.microsoft.com/ef/ | SQLite for development/testing |
| EF Tools | Microsoft.EntityFrameworkCore.Tools | 9.0.8 | MIT | https://docs.microsoft.com/ef/ | Migrations and scaffolding |
| EF Design | Microsoft.EntityFrameworkCore.Design | 9.0.8 | MIT | https://docs.microsoft.com/ef/ | Design-time components |
| **API DOCUMENTATION** |
| OpenAPI | Microsoft.AspNetCore.OpenApi | 9.0.8 | MIT | https://github.com/dotnet/aspnetcore | OpenAPI specification generation |
| Swagger UI | Swashbuckle.AspNetCore | 6.9.0 | MIT | https://github.com/domaindrivendev/Swashbuckle.AspNetCore | API documentation UI |
| **PYTHON - AI WORKERS** |
| Web Framework | FastAPI | 0.115.0 | MIT | https://fastapi.tiangolo.com/ | HTTP APIs for AI workers |
| ASGI Server | Uvicorn | 0.32.0 | BSD-3-Clause | https://www.uvicorn.org/ | ASGI web server |
| Data Validation | Pydantic | 2.9.2 | MIT | https://docs.pydantic.dev/ | Request/response validation |
| Settings Management | Pydantic-settings | 2.5.2 | MIT | https://docs.pydantic.dev/ | Environment configuration |
| Environment Variables | python-dotenv | 1.0.1 | BSD-3-Clause | https://github.com/theskumar/python-dotenv | .env file loading |
| **AI & MACHINE LEARNING** |
| Transformer Models | transformers | Latest (via optimum) | Apache 2.0 | https://huggingface.co/transformers | HuggingFace model loading |
| Model Optimization | optimum[onnxruntime] | Latest | Apache 2.0 | https://huggingface.co/docs/optimum | ONNX runtime optimization |
| ONNX Runtime | onnxruntime | Latest (via optimum) | MIT | https://onnxruntime.ai/ | Optimized inference |
| Deep Learning | torch | Latest (via optimum) | BSD-3-Clause | https://pytorch.org/ | PyTorch backend |
| ONNX Format | onnx | Latest (via optimum) | Apache 2.0 | https://onnx.ai/ | Model format |
| Embedding Model | truro7/vn-law-embedding | - | Unknown | https://huggingface.co/truro7/vn-law-embedding | Vietnamese legal document embeddings (768-dim) |
| LLM Model | ontocord/vistral:latest | - | Unknown | https://ollama.com/ | Vietnamese-finetuned language model |
| RAG Evaluation | ragas | Latest | Apache 2.0 | https://github.com/explodinggradients/ragas | RAG pipeline metrics |
| Dataset Handling | datasets | Latest | Apache 2.0 | https://huggingface.co/docs/datasets | Dataset management |
| **VECTOR DATABASE** |
| Vector DB | Qdrant | latest | Apache 2.0 | https://qdrant.tech/ | Vector similarity search |
| Python Client | qdrant-client | 1.11.3 | Apache 2.0 | https://github.com/qdrant/qdrant-client | Qdrant API client |
| .NET Client | Qdrant.Client | 1.16.1 | Apache 2.0 | https://github.com/qdrant/qdrant-dotnet | Qdrant .NET client |
| **MESSAGE QUEUE** |
| Message Broker | RabbitMQ | 3-management | MPL 2.0 | https://www.rabbitmq.com/ | Event-driven messaging |
| Async Python Client | aio-pika | 9.4.3 | Apache 2.0 | https://aio-pika.readthedocs.io/ | Async RabbitMQ client for Python |
| .NET Client | MassTransit.RabbitMQ | 8.3.4 | Apache 2.0 | https://masstransit-project.com/ | RabbitMQ abstraction for .NET |
| **LLM INFERENCE** |
| LLM Server | Ollama | latest | MIT | https://ollama.com/ | Local LLM hosting |
| **OBJECT STORAGE** |
| S3-Compatible Storage | MinIO | latest | AGPL-3.0 | https://min.io/ | Object storage server |
| .NET Client | Minio | 7.0.0 | Apache 2.0 | https://min.io/docs/minio/linux/developers/dotnet/minio-dotnet.html | MinIO SDK for .NET |
| **AUTHENTICATION** |
| JWT | Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.8 | MIT | https://github.com/dotnet/aspnetcore | JWT authentication middleware |
| Python JWT | PyJWT | 2.8.0 | MIT | https://pyjwt.readthedocs.io/ | JWT encoding/decoding in Python |
| Password Hashing | BCrypt | (built-in) | Custom | - | Secure password hashing |
| **HTTP CLIENTS** |
| Python HTTP Client | httpx | 0.27.0 | BSD-3-Clause | https://www.python-httpx.org/ | Async HTTP client for Python |
| **FRONTEND** |
| View Engine | Razor | 9.0 (ASP.NET Core) | MIT | https://docs.microsoft.com/aspnet/core/mvc/views/razor | Server-side rendering |
| JavaScript Library | jQuery | 3.x | MIT | https://jquery.com/ | DOM manipulation |
| CSS Framework | Bootstrap | 5.x | MIT | https://getbootstrap.com/ | Responsive UI framework |
| Real-Time Client | SignalR JavaScript Client | 8.0.0 | MIT | https://www.npmjs.com/package/@microsoft/signalr | WebSocket client |
| Form Validation | jQuery Validation | latest | MIT | https://jqueryvalidation.org/ | Client-side validation |
| MVC Validation | jQuery Validation Unobtrusive | latest | Apache 2.0 | https://github.com/aspnet/jquery-validation-unobtrusive | ASP.NET MVC integration |
| **CONTAINERIZATION** |
| Container Platform | Docker | latest | Apache 2.0 | https://www.docker.com/ | Application containerization |
| Orchestration | Docker Compose | latest | Apache 2.0 | https://docs.docker.com/compose/ | Multi-container deployment |
| **DEVELOPMENT TOOLS** |
| IDE | Visual Studio 2022 / VS Code | latest | Proprietary / MIT | https://visualstudio.microsoft.com/ | Development environment |
| Version Control | Git | latest | GPL-2.0 | https://git-scm.com/ | Source code management |
| Package Manager (.NET) | NuGet | latest | Apache 2.0 | https://www.nuget.org/ | .NET package management |
| Package Manager (Python) | pip | latest | MIT | https://pip.pypa.io/ | Python package management |
| **TESTING (Recommended - Not Yet Implemented)** |
| Unit Testing (.NET) | xUnit | TBD | Apache 2.0 | https://xunit.net/ | Recommended for .NET testing |
| Mocking (.NET) | Moq | TBD | BSD-3-Clause | https://github.com/moq/moq4 | Recommended for .NET mocking |
| Unit Testing (Python) | pytest | TBD | MIT | https://pytest.org/ | Recommended for Python testing |
| JavaScript Testing | Jest | TBD | MIT | https://jestjs.io/ | Recommended for frontend testing |

---

## TECHNOLOGY STACK SUMMARY BY LAYER

### Backend Services (.NET 9)
- **Framework:** ASP.NET Core 9.0 (Minimal APIs)
- **ORM:** Entity Framework Core 9.0
- **Message Bus:** MassTransit 8.3.4 + RabbitMQ
- **Real-Time:** SignalR 1.1.0
- **Background Jobs:** Hangfire 1.8.17
- **API Gateway:** YARP 2.3.0
- **Logging:** Serilog 10.0.0

### AI Workers (Python)
- **Framework:** FastAPI 0.115.0 + Uvicorn 0.32.0
- **ML Libraries:** HuggingFace Transformers + ONNX Runtime + PyTorch
- **Embedding Model:** truro7/vn-law-embedding (768-dim)
- **LLM:** ontocord/vistral:latest (Ollama)
- **RAG Evaluation:** RAGAS
- **Message Queue:** aio-pika 9.4.3

### Frontend
- **Framework:** ASP.NET Core MVC 9.0 (Razor)
- **JavaScript:** jQuery 3.x
- **UI Framework:** Bootstrap 5
- **Real-Time:** SignalR JavaScript Client 8.0.0
- **Validation:** jQuery Validation + Unobtrusive

### Infrastructure
- **Database:** SQL Server 2022
- **Vector DB:** Qdrant (latest)
- **Message Queue:** RabbitMQ 3
- **Object Storage:** MinIO (latest)
- **LLM Server:** Ollama (latest)
- **Container:** Docker + Docker Compose

---

## LICENSE COMPLIANCE SUMMARY

| License Type | Count | Libraries |
|--------------|-------|-----------|
| **MIT** | 35+ | Most .NET and JavaScript libraries |
| **Apache 2.0** | 15+ | MassTransit, Serilog, RabbitMQ clients, AI libraries |
| **LGPL-3.0** | 1 | Hangfire (dual-licensed, commercial available) |
| **BSD-3-Clause** | 3 | Uvicorn, python-dotenv, PyTorch |
| **MPL 2.0** | 1 | RabbitMQ server |
| **AGPL-3.0** | 1 | MinIO server (enterprise license available) |
| **Proprietary** | 2 | SQL Server (dev edition free), Visual Studio |

**Note:** All licenses are compatible with academic/research use. For commercial deployment, review Hangfire and MinIO licensing.

---

## VERSION COMPATIBILITY MATRIX

| Component | Minimum Version | Tested Version | Compatible With |
|-----------|-----------------|----------------|-----------------|
| .NET Runtime | 9.0 | 9.0 | Windows, Linux, macOS |
| Python | 3.9+ | 3.11+ | Linux recommended for AI workers |
| SQL Server | 2019 | 2022 | Docker on any OS |
| Docker | 20.10+ | latest | Docker Desktop / Linux Docker |
| Node.js (for build tools) | N/A | N/A | Not required (CDN used) |

---

## SECURITY LIBRARIES & PRACTICES

| Purpose | Technology | Implementation |
|---------|-----------|----------------|
| Password Hashing | BCrypt | AccountService password storage |
| JWT Generation | System.IdentityModel.Tokens.Jwt | TokenClaimsService |
| JWT Validation (.NET) | Microsoft.AspNetCore.Authentication.JwtBearer | All services |
| JWT Validation (Python) | PyJWT | ChatProcessor, EmbeddingService |
| HTTPS | Kestrel | Production deployment (not in docker-compose dev) |
| CORS | Microsoft.AspNetCore.Cors | ApiGateway |
| Authentication | Cookie + JWT Bearer | Hybrid approach |
| Authorization | Policy-based | scope_web, scope_mobile, admin |

---

## DEPLOYMENT STACK

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Containerization | Docker | Application isolation |
| Orchestration | Docker Compose | Multi-container management |
| Networking | Docker Bridge Network | Service communication |
| Volumes | Docker Volumes | Data persistence |
| Environment Config | appsettings.json + .env | Configuration management |
| Service Discovery | Docker DNS | Service name resolution |

---

## RECOMMENDED ADDITIONS (Not Yet Implemented)

| Purpose | Recommended Technology | Benefit |
|---------|----------------------|---------|
| Caching | Redis | Performance improvement |
| Rate Limiting | AspNetCoreRateLimit | DDoS protection |
| API Versioning | Microsoft.AspNetCore.Mvc.Versioning | Backward compatibility |
| Health Checks | Microsoft.Extensions.Diagnostics.HealthChecks | Monitoring |
| Monitoring | Application Insights / Grafana | Observability |
| CI/CD | GitHub Actions / Azure DevOps | Automation |
| Secret Management | Azure Key Vault / HashiCorp Vault | Security |
| Unit Testing | xUnit + Moq + pytest | Code quality |

---

**END OF TECHNOLOGY INVENTORY**

---

**Usage in Thesis:**
- **Chapter 3:** Copy relevant sections for technology overview
- **Chapter 4:** Reference for "Tools and Libraries Used" section
- **Appendix:** Include full table for comprehensive reference
