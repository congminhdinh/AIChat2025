# PHASE 3: Class Analysis for 4.1.3 (UML Class Diagrams)

## INPUT REQUIRED
Đọc 2 files:
1. `project_map.md` (Phase 1)
2. `package_analysis_4_1_2.md` (Phase 2)

## MỤC TIÊU
Tạo outline cho **2 UML Class Diagrams** trong 4.1.3:
1. **Diagram 1**: Multi-tenant Isolation (Infrastructure-centric)
2. **Diagram 2**: RAG Pipeline

**YÊU CẦU:**
- CHỈ tên class, KHÔNG methods/properties
- Relationships theo UML standard
- Evidence: file:line only (NO code snippets)
- TEXT-BASED output, NO PlantUML/Mermaid

---

## DIAGRAM 1: MULTI-TENANT ISOLATION (Infrastructure-centric)

### SCOPE
```
TRỌNG TÂM: Infrastructure (Shared Library)
PHỤ: ApiGateway, AccountService, TenantService
```

### ANALYSIS STRATEGY

**Step 1: Infrastructure Package (PRIORITY)**

Scan: `./Infrastructure/Infrastructure.csproj`

Find classes:
```bash
# Tìm TenantContext và utilities
find ./Infrastructure -name "*Tenant*.cs"
find ./Infrastructure -name "*Context*.cs"

# Với MỖI class:
# 1. Tên class
# 2. Namespace
# 3. Base class/interfaces (grep "public class.*:")
# 4. Dependencies (grep "private readonly")
# 5. Properties (grep "public.*{ get; set; }")
```

Expected classes:
- TenantContextAccessor
- ITenantContextAccessor
- TenantInfo
- CurrentTenantMiddleware
- Extensions (DI registration)

**Step 2: ApiGateway**

Scan: `./ApiGateway/ApiGateway.csproj`

Find:
- Config files (appsettings.json - routing only)
- NO middleware classes (routing-only gateway)

**Step 3: AccountService** 

Scan: `./AccountService/AccountService.csproj`

Find classes in:
```
Controllers/
├── UserController.cs
├── AuthController.cs
└── AccountController.cs

DbContext/
└── AccountDbContext.cs (SHARED AIChat2025 DB)
```

**Step 4: TenantService**

Scan: `./TenantService/TenantService.csproj`

Find:
```
Controllers/
└── TenantController.cs

DbContext/
└── TenantDbContext.cs (SHARED AIChat2025 DB)
```

---

### OUTPUT FORMAT

```markdown
# DIAGRAM 1: MULTI-TENANT ISOLATION

## Infrastructure Package (Core)

### CLASS: TenantContextAccessor
File: ./Infrastructure/TenantContextAccessor.cs
Type: Service class [MULTITENANT]

Relationships:
- TenantContextAccessor --▷ ITenantContextAccessor
  Evidence: Line X
- TenantContextAccessor -----> IHttpContextAccessor
  Evidence: Line Y

### CLASS: ITenantContextAccessor
File: ./Infrastructure/ITenantContextAccessor.cs
Type: Interface [MULTITENANT]

### CLASS: TenantInfo
File: ./Infrastructure/TenantInfo.cs
Type: Value Object [MULTITENANT]

Properties: TenantId, TenantName
(NO details on properties in diagram)

### CLASS: CurrentTenantMiddleware
File: ./Infrastructure/Middleware/CurrentTenantMiddleware.cs
Type: Middleware [MULTITENANT]

Relationships:
- CurrentTenantMiddleware -----> ITenantContextAccessor
  Evidence: Line Z
- CurrentTenantMiddleware -----> HttpContext
  Evidence: Line W

---

## AccountService Package

### CLASS: UserController
File: ./AccountService/Controllers/UserController.cs
Type: Controller

Relationships:
- UserController -----> IAccountService
  Evidence: Line A

### CLASS: AccountDbContext
File: ./AccountService/Data/AccountDbContext.cs
Type: DbContext [MULTITENANT]

Relationships:
- AccountDbContext ──▷ DbContext
  Evidence: Line B
- AccountDbContext -----> ITenantContextAccessor [FROM INFRASTRUCTURE]
  Evidence: Line C (OnModelCreating - applies tenant filter)

---

## TenantService Package

### CLASS: TenantController
File: ./TenantService/Controllers/TenantController.cs
Type: Controller

### CLASS: TenantDbContext
File: ./TenantService/Data/TenantDbContext.cs
Type: DbContext

Relationships:
- TenantDbContext ──▷ DbContext
  Evidence: Line D

---

## CROSS-PACKAGE DEPENDENCIES

1. AccountDbContext → ITenantContextAccessor (Infrastructure)
   Type: Dependency [MULTITENANT CRITICAL]
   Evidence: Line C
   Purpose: Apply global query filter WHERE TenantId = CurrentTenantId

2. ChatDbContext → ITenantContextAccessor (Infrastructure)
   Type: Dependency [MULTITENANT CRITICAL]
   Evidence: Line E
   Purpose: Same as above

3. All Controllers → ITenantContextAccessor (Infrastructure)
   Type: Dependency [MULTITENANT]
   Evidence: Constructor injection
   Purpose: Access current tenant context

---

## DIAGRAM LAYOUT

```
┌────────────────────────────────────────────────────┐
│         INFRASTRUCTURE (Shared Library)            │
│                                                    │
│  ┌────────────────────┐   ┌──────────────────┐   │
│  │ITenantContextAccessor│◄──│TenantContextAccessor│   │
│  └────────────────────┘   └──────────────────┘   │
│            ▲                       ▲              │
│            │                       │              │
│  ┌─────────────────┐   ┌──────────────────────┐ │
│  │ TenantInfo      │   │CurrentTenantMiddleware│ │
│  └─────────────────┘   └──────────────────────┘ │
└────────────────────────────────────────────────────┘
         │                           │
         │ <<use>>                   │ <<use>>
         ▼                           ▼
┌───────────────────────┐   ┌───────────────────────┐
│  AccountService       │   │  TenantService        │
│                       │   │                       │
│ ┌─────────────────┐  │   │ ┌─────────────────┐  │
│ │UserController   │  │   │ │TenantController │  │
│ └─────────────────┘  │   │ └─────────────────┘  │
│                       │   │                       │
│ ┌─────────────────┐  │   │ ┌─────────────────┐  │
│ │AccountDbContext │──┼───┼─│TenantDbContext  │  │
│ │[Uses ITenant... ]│  │   │ └─────────────────┘  │
│ └─────────────────┘  │   │                       │
└───────────────────────┘   └───────────────────────┘
```

---

## STATISTICS
- Total Classes: [count]
- [MULTITENANT] Classes: [count]
- Cross-package Dependencies: [count]
```

---

## DIAGRAM 2: RAG PIPELINE

### SCOPE
```
ChatService (.NET)
ChatProcessor (Python)
EmbeddingService (Python)
```

### ANALYSIS STRATEGY

**Step 1: ChatService**

Scan: `./ChatService/ChatService.csproj`

Find classes:
```bash
# Controllers
find ./ChatService/Controllers -name "*.cs"

# Hubs (SignalR)
find ./ChatService/Hubs -name "*Hub.cs"

# Services
find ./ChatService/Services -name "*Service.cs"

# Consumers
find ./ChatService/Consumers -name "*Consumer.cs"

# DbContext
find ./ChatService/Data -name "*DbContext.cs"
```

Expected:
- ChatController [RAG]
- ChatHub (SignalR) [RAG]
- IChatService
- ChatService
- BotResponseConsumer [RAG]
- ChatDbContext

**Step 2: ChatProcessor (Python)**

Scan: `./ChatProcessor/src/`

Find:
```bash
# Services
ls ./ChatProcessor/src/services/*.py

# Clients
ls ./ChatProcessor/src/clients/*.py
```

Expected:
- query_expander.py [RAG]
- hybrid_searcher.py [RAG]
- llm_generator.py [RAG]
- qdrant_client.py [RAG]
- rabbitmq_consumer.py

**Step 3: EmbeddingService (Python)**

Scan: `./EmbeddingService/src/`

Expected:
- hierarchical_chunker.py [RAG]
- vector_generator.py [RAG]
- minio_client.py
- qdrant_client.py [RAG]

---

### OUTPUT FORMAT

```markdown
# DIAGRAM 2: RAG PIPELINE

## ChatService Package (.NET)

### CLASS: ChatController
File: ./ChatService/Controllers/ChatController.cs
Type: Controller [RAG]

Relationships:
- ChatController -----> IChatService
  Evidence: Line X

### CLASS: ChatHub
File: ./ChatService/Hubs/ChatHub.cs
Type: SignalR Hub [RAG]

Relationships:
- ChatHub ──▷ Hub
  Evidence: Line Y
- ChatHub -----> IChatService
  Evidence: Line Z

### CLASS: ChatService
File: ./ChatService/Services/ChatService.cs
Type: Service [RAG]

Relationships:
- ChatService --▷ IChatService
  Evidence: Line A
- ChatService -----> IPublishEndpoint (MassTransit)
  Evidence: Line B (publishes to RabbitMQ)

### CLASS: BotResponseConsumer
File: ./ChatService/Consumers/BotResponseConsumer.cs
Type: Consumer [RAG]

Relationships:
- BotResponseConsumer --▷ IConsumer<BotResponse>
  Evidence: Line C
- BotResponseConsumer -----> IHubContext<ChatHub>
  Evidence: Line D (sends to SignalR clients)

### CLASS: ChatDbContext
File: ./ChatService/Data/ChatDbContext.cs
Type: DbContext [MULTITENANT]

Relationships:
- ChatDbContext ──▷ DbContext
  Evidence: Line E
- ChatDbContext -----> ITenantContextAccessor
  Evidence: Line F

---

## ChatProcessor Package (Python)

### MODULE: query_expander
File: ./ChatProcessor/src/services/query_expander.py
Type: Python module [RAG]

CLASS: QueryExpander

Relationships:
- QueryExpander -----> Dict
  Evidence: Line G

### MODULE: hybrid_searcher
File: ./ChatProcessor/src/services/hybrid_searcher.py
Type: Python module [RAG]

CLASS: HybridSearcher

Relationships:
- HybridSearcher -----> QdrantClient
  Evidence: Line H
- HybridSearcher -----> BM25Okapi (rank_bm25)
  Evidence: Line I

### MODULE: llm_generator
File: ./ChatProcessor/src/services/llm_generator.py
Type: Python module [RAG]

CLASS: LLMGenerator

Relationships:
- LLMGenerator -----> OllamaClient
  Evidence: Line J

### MODULE: qdrant_client
File: ./ChatProcessor/src/clients/qdrant_client.py
Type: Python module [RAG]

CLASS: QdrantClientWrapper

Relationships:
- QdrantClientWrapper -----> QdrantClient (library)
  Evidence: Line K

### MODULE: rabbitmq_consumer
File: ./ChatProcessor/src/clients/rabbitmq_consumer.py
Type: Python module

CLASS: RabbitMQConsumer

Relationships:
- RabbitMQConsumer -----> pika.BlockingConnection
  Evidence: Line L

---

## EmbeddingService Package (Python)

### MODULE: hierarchical_chunker
File: ./EmbeddingService/src/services/hierarchical_chunker.py
Type: Python module [RAG]

CLASS: HierarchicalChunker

Relationships:
- HierarchicalChunker -----> re
  Evidence: Line M (regex for hierarchy detection)

### MODULE: vector_generator
File: ./EmbeddingService/src/services/vector_generator.py
Type: Python module [RAG]

CLASS: VectorGenerator

Relationships:
- VectorGenerator -----> SentenceTransformer
  Evidence: Line N

### MODULE: minio_client
File: ./EmbeddingService/src/clients/minio_client.py
Type: Python module

CLASS: MinIOClientWrapper

Relationships:
- MinIOClientWrapper -----> Minio (library)
  Evidence: Line O

---

## CROSS-PACKAGE DEPENDENCIES

1. ChatService.ChatService → RabbitMQ
   Type: Async Publish [RAG]
   Evidence: Line P
   Message: ChatQueryMessage

2. ChatProcessor.RabbitMQConsumer → RabbitMQ
   Type: Async Consume [RAG]
   Evidence: Line Q

3. ChatProcessor.HybridSearcher → Qdrant
   Type: Vector Search [RAG]
   Evidence: Line R

4. ChatProcessor.LLMGenerator → Ollama
   Type: LLM API Call [RAG]
   Evidence: Line S

5. EmbeddingService.VectorGenerator → Qdrant
   Type: Vector Store [RAG]
   Evidence: Line T

6. ChatProcessor → ChatService (via RabbitMQ)
   Type: Response Publish [RAG]
   Evidence: Line U
   Message: BotResponseMessage → BotResponseConsumer

---

## DIAGRAM LAYOUT

```
┌─────────────────────────────────────────────┐
│         ChatService (.NET)                  │
│                                             │
│  ┌──────────┐  ┌──────┐  ┌──────────────┐ │
│  │ChatController│──│ChatHub│──│ChatService│ │
│  └──────────┘  └──────┘  └──────────────┘ │
│                              │              │
│                              │ publish      │
│  ┌──────────────────┐       ▼              │
│  │BotResponseConsumer│   [RabbitMQ]        │
│  └──────────────────┘       │              │
└─────────────────────────────┼───────────────┘
                              │
              ┌───────────────┼───────────────┐
              │ consume       │ publish       │
              ▼               ▼               │
┌──────────────────────────────────────────────┐
│      ChatProcessor (Python)                  │
│                                              │
│  ┌──────────────┐  ┌──────────────┐        │
│  │QueryExpander │  │HybridSearcher│───┐    │
│  └──────────────┘  └──────────────┘   │    │
│                                        │    │
│  ┌──────────────┐                     ▼    │
│  │LLMGenerator  │              ┌──────────┐│
│  └──────────────┘              │Qdrant    ││
│         │                      │Client    ││
│         ▼                      └──────────┘│
│  ┌──────────────┐                          │
│  │Ollama Client │                          │
│  └──────────────┘                          │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│     EmbeddingService (Python)                │
│                                              │
│  ┌──────────────────┐  ┌──────────────┐    │
│  │HierarchicalChunker│  │VectorGenerator│   │
│  └──────────────────┘  └──────────────┘    │
│         │                     │             │
│         ▼                     ▼             │
│  ┌──────────┐         ┌──────────┐         │
│  │MinIO     │         │Qdrant    │         │
│  │Client    │         │Client    │         │
│  └──────────┘         └──────────┘         │
└──────────────────────────────────────────────┘
```

---

## STATISTICS
- Total Classes: [count]
- .NET Classes: [count]
- Python Modules: [count]
- [RAG] Classes: [count]
- Async Dependencies: [count]
```

---

## EXECUTION INSTRUCTIONS

### Phase 1: Scan Infrastructure (PRIORITY)
```bash
cd ./Infrastructure
find . -name "*.cs" | xargs grep -l "Tenant"
# List ALL classes found
# For EACH class: name, namespace, relationships (evidence: file:line)
```

### Phase 2: Scan Services
```bash
# For AccountService, TenantService
cd ./[ServiceName]
find . -name "*Controller.cs"
find . -name "*DbContext.cs"
# List classes, relationships (evidence only)
```

### Phase 3: Scan ChatService
```bash
cd ./ChatService
find . -name "*.cs"
# Focus: Controllers, Hubs, Services, Consumers
```

### Phase 4: Scan Python Services
```bash
cd ./ChatProcessor/src
ls services/*.py clients/*.py
# For EACH .py: main class, imports (dependencies)

cd ./EmbeddingService/src
ls services/*.py clients/*.py
```

### Output Rules
1. **Class format**: Name | File | Type | Evidence (line only)
2. **Relationship format**: ClassA → ClassB | Type | Evidence (line only)
3. **NO code snippets** (save tokens)
4. **NO explanations** beyond 1 sentence per class
5. **Focus on structure**, not implementation

---

## CRITICAL REQUIREMENTS

1. ✅ Infrastructure package = TRỌNG TÂM của Diagram 1
2. ✅ Shared database model (AIChat2025 DB)
3. ✅ ITenantContextAccessor usage across all DbContexts
4. ✅ Python modules = classes in UML
5. ✅ Evidence: file:line ONLY, NO code snippets
6. ✅ TEXT-BASED output
7. ✅ Minimize explanations (save tokens)

---

START ANALYSIS NOW.

Generate: `class_analysis_4_1_3.md`
