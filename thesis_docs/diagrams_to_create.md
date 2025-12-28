# Diagrams to Create for AIChat2025 Thesis

**Generated:** 2025-12-28
**Total Diagrams:** 28 diagrams
**Primary Tool:** PlantUML
**Alternative Tools:** Draw.io, Mermaid (for complex architecture)

---

## CHAPTER 1 - GIỚI THIỆU (Introduction)

### 1. Use Case Diagram - System Overview
- **Type:** UML Use Case Diagram
- **Tool:** PlantUML
- **File:** `diagrams/use_case_overview.puml`
- **Priority:** HIGH
- **Content:**
  - Actors: Super Admin, Tenant Admin, End User
  - Use cases: Login, Chat Consultation, Upload Document, Manage Users, Configure System
  - System boundary
  - <<include>> and <<extend>> relationships
- **Estimated Size:** Full page
- **For:** Section 1.1 (Background)

### 2. System Context Diagram (C4 Level 1)
- **Type:** C4 Architecture - Context
- **Tool:** PlantUML (C4 extension)
- **File:** `diagrams/system_architecture_context.puml`
- **Priority:** HIGH
- **Content:**
  - AIChat2025 system as central box
  - External users: Super Admin, Tenant Admin, End User
  - External systems: None (self-contained)
  - Technology labels
- **Estimated Size:** Half page
- **For:** Section 1.2 (Objectives)

---

## CHAPTER 2 - KHẢO SÁT VÀ PHÂN TÍCH (Survey and Analysis)

### 3. Comparison Matrix - Legal Chatbot Features
- **Type:** Table/Matrix
- **Tool:** LaTeX table or Markdown
- **Priority:** MEDIUM
- **Content:**
  - AIChat2025 vs competitors (DoNotPay, LawGeex, ROSS Intelligence)
  - Features: Multi-tenant, RAG, Vietnamese support, Document upload, Real-time, Dual-RAG
  - Comparison checkmarks
- **Estimated Size:** Full page table
- **For:** Section 2.1.1 (Survey of existing systems)

### 4. Activity Diagram - Chat Consultation Flow (Main Use Case)
- **Type:** UML Activity Diagram
- **Tool:** PlantUML
- **File:** `diagrams/activity_chat_consultation.puml`
- **Priority:** HIGH
- **Content:**
  - User submits query
  - System expands query with PromptConfig
  - Parallel: Search company rules + legal base
  - Scenario detection
  - Context structuring
  - LLM generation
  - Response cleanup
  - Return to user
  - Decision nodes for scenario (BOTH/COMPANY_ONLY/LEGAL_ONLY/NONE)
- **Estimated Size:** Full page
- **For:** Section 2.3 (Detailed Use Case Specification)

### 5. Activity Diagram - Document Upload & Vectorization
- **Type:** UML Activity Diagram
- **Tool:** PlantUML
- **File:** `diagrams/activity_document_upload.puml`
- **Priority:** MEDIUM
- **Content:**
  - User uploads .docx file
  - Standardize headings
  - Store in MinIO
  - Extract hierarchical chunks
  - Background job enqueued
  - Batch vectorization
  - Store in Qdrant
  - Update document status
- **Estimated Size:** Full page
- **For:** Section 2.3 (Detailed Use Case Specification)

### 6. Activity Diagram - User Authentication
- **Type:** UML Activity Diagram
- **Tool:** PlantUML
- **Priority:** LOW
- **Content:**
  - Enter credentials
  - Validate tenant + user
  - Check password (BCrypt)
  - Generate JWT token
  - Store in cookie
  - Redirect to chat
- **Estimated Size:** Half page
- **For:** Section 2.3 (Detailed Use Case Specification)

---

## CHAPTER 3 - CÁC CÔNG NGHỆ SỬ DỤNG (Technologies Used)

### 7. Technology Stack Diagram
- **Type:** Architecture Diagram
- **Tool:** PlantUML or Draw.io
- **Priority:** HIGH
- **Content:**
  - Layered architecture showing:
    - Frontend: ASP.NET MVC, Razor, jQuery, Bootstrap, SignalR Client
    - API Gateway: YARP
    - Backend Services: .NET 9 microservices
    - AI Workers: Python FastAPI
    - Infrastructure: SQL Server, RabbitMQ, Qdrant, Ollama, MinIO
  - Technology logos and versions
- **Estimated Size:** Full page
- **For:** Section 3.1-3.7 (Technology overview)

### 8. RAG Framework Overview
- **Type:** Component Diagram
- **Tool:** PlantUML
- **Priority:** MEDIUM
- **Content:**
  - Query → Embedding → Vector Search → Context → LLM → Response
  - Components: EmbeddingService, QdrantService, OllamaService, ChatBusiness
- **Estimated Size:** Half page
- **For:** Section 3.2.2 (LangChain/RAG Framework)

---

## CHAPTER 4 - THIẾT KẾ VÀ TRIỂN KHAI (Design and Implementation)

### 9. System Architecture - Container Diagram (C4 Level 2)
- **Type:** C4 Architecture - Container
- **Tool:** PlantUML (C4 extension)
- **File:** `diagrams/system_architecture_container.puml`
- **Priority:** HIGH
- **Content:**
  - Web Browser → WebApp (ASP.NET MVC)
  - WebApp → ApiGateway (YARP)
  - ApiGateway → 5 Backend Services (.NET)
  - ChatService → RabbitMQ
  - RabbitMQ → ChatProcessor (Python)
  - ChatProcessor → Qdrant + Ollama
  - DocumentService → Hangfire → EmbeddingService → Qdrant
  - All services → SQL Server
  - Technology labels on each container
- **Estimated Size:** Full page
- **For:** Section 4.1.1 (Overall Architecture)

### 10. System Architecture - Component Diagram (C4 Level 3) - ChatService
- **Type:** C4 Architecture - Component
- **Tool:** PlantUML
- **Priority:** MEDIUM
- **Content:**
  - ChatHub (SignalR)
  - ChatBusiness (logic)
  - ChatRepository (data access)
  - BotResponseConsumer (RabbitMQ)
  - UserPromptPublisher (RabbitMQ)
  - ChatDbContext (EF Core)
- **Estimated Size:** Half page
- **For:** Section 4.1.1 (Overall Architecture)

### 11. Multi-Tenant Data Flow Diagram
- **Type:** Sequence + Flow Diagram Hybrid
- **Tool:** PlantUML
- **File:** `diagrams/multi_tenant_data_flow.puml`
- **Priority:** HIGH
- **Content:**
  - User login → JWT with TenantId claim
  - HTTP Request → CurrentTenantProvider extracts TenantId
  - Write: UpdateTenancyInterceptor auto-sets TenantId
  - Read: TenancySpecification filters by TenantId
  - Background: JWT decoded → SetTenantId manually
  - Show data isolation at each layer
- **Estimated Size:** Full page
- **For:** Section 4.1.2 (Multi-tenant Architecture)

### 12. RAG Pipeline Sequence Diagram
- **Type:** UML Sequence Diagram
- **Tool:** PlantUML
- **File:** `diagrams/rag_pipeline_sequence.puml`
- **Priority:** HIGH
- **Content:**
  - User → ChatService: SendMessage
  - ChatService → RabbitMQ: Publish UserPromptReceived
  - RabbitMQ → ChatProcessor: Consume message
  - ChatProcessor → EmbeddingService: Get embedding
  - ChatProcessor → Qdrant: Search (parallel: company + legal)
  - ChatProcessor: Scenario detection
  - ChatProcessor: Context structuring
  - ChatProcessor → Ollama: Generate response
  - ChatProcessor: Cleanup response
  - ChatProcessor → RabbitMQ: Publish BotResponseCreated
  - RabbitMQ → ChatService: Consume response
  - ChatService → SignalR: Broadcast to clients
  - Show all 9 steps clearly
- **Estimated Size:** Full page
- **For:** Section 4.1.3 (RAG Pipeline Architecture) - cross-reference to Chapter 5

### 13. Authentication Sequence Diagram
- **Type:** UML Sequence Diagram
- **Tool:** PlantUML
- **File:** `diagrams/authentication_sequence.puml`
- **Priority:** MEDIUM
- **Content:**
  - User → WebApp: Submit login form
  - WebApp → ApiGateway: POST /auth/login
  - ApiGateway → AccountService: Forward request
  - AccountService → Database: Validate user + tenant
  - AccountService: Verify password (BCrypt)
  - AccountService: Generate JWT token
  - AccountService → WebApp: Return token
  - WebApp: Store token in cookie
  - WebApp → User: Redirect to chat
- **Estimated Size:** Half page
- **For:** Section 4.2.2 (Class Design) - Authentication module

### 14. Document Embedding Sequence Diagram
- **Type:** UML Sequence Diagram
- **Tool:** PlantUML
- **File:** `diagrams/document_embedding_sequence.puml`
- **Priority:** MEDIUM
- **Content:**
  - User → DocumentService: Upload .docx
  - DocumentService → MinIO: Store file
  - DocumentService: Standardize headings
  - DocumentService: Extract hierarchical chunks
  - DocumentService → Hangfire: Enqueue VectorizeBackgroundJob
  - Hangfire → VectorizeBackgroundJob: Execute
  - VectorizeBackgroundJob → EmbeddingService: POST /vectorize-batch
  - EmbeddingService: Encode text (768-dim vectors)
  - EmbeddingService → Qdrant: Store vectors with metadata
  - Qdrant → VectorizeBackgroundJob: Success
  - VectorizeBackgroundJob → Database: Update document status
- **Estimated Size:** Full page
- **For:** Section 4.2.2 (Class Design) - Document module

### 15. Class Diagram - Authentication Module
- **Type:** UML Class Diagram
- **Tool:** PlantUML
- **File:** `diagrams/class_diagram_auth.puml`
- **Priority:** HIGH
- **Content:**
  - Classes: Account (entity), AccountBusiness, AccountRepository, TokenClaimsService, CurrentUserProvider, CurrentTenantProvider
  - Relationships: inheritance, composition, dependencies
  - Key methods and properties
  - Stereotypes: <<entity>>, <<service>>, <<repository>>
- **Estimated Size:** Full page
- **For:** Section 4.2.2 (Class Design)

### 16. Class Diagram - Chat Module
- **Type:** UML Class Diagram
- **Tool:** PlantUML
- **Priority:** HIGH
- **Content:**
  - Classes: ChatConversation, ChatMessage, ChatBusiness, ChatRepository, ChatHub, BotResponseConsumer
  - Relationships
  - Key methods
- **Estimated Size:** Full page
- **For:** Section 4.2.2 (Class Design)

### 17. Class Diagram - Document Module
- **Type:** UML Class Diagram
- **Tool:** PlantUML
- **Priority:** MEDIUM
- **Content:**
  - Classes: PromptDocument, PromptDocumentBusiness, VectorizeBackgroundJob, StorageBusiness
  - Relationships
  - Key methods
- **Estimated Size:** Half page
- **For:** Section 4.2.2 (Class Design)

### 18. Database ER Diagram (Complete)
- **Type:** Entity-Relationship Diagram
- **Tool:** PlantUML
- **File:** `diagrams/database_er_diagram.puml`
- **Priority:** HIGH
- **Content:**
  - All entities: Tenant, Account, Permission, ChatConversation, ChatMessage, PromptConfig, PromptDocument
  - Primary keys, foreign keys
  - Relationships (1:N, N:M if any)
  - Data types for key fields
  - Indexes
- **Estimated Size:** Full page
- **For:** Section 4.2.3 (Database Design)

### 19. Deployment Diagram
- **Type:** UML Deployment Diagram
- **Tool:** PlantUML
- **File:** `diagrams/deployment_diagram.puml`
- **Priority:** HIGH
- **Content:**
  - Physical nodes: Docker Host
  - Containers: All 13 services
  - Networks: aichat-network (bridge)
  - Volumes: Qdrant, Ollama, MinIO
  - Port mappings
  - Dependencies
- **Estimated Size:** Full page
- **For:** Section 4.5.1 (Deployment Environment)

### 20. UI Wireframe - Login Page
- **Type:** Wireframe/Mockup
- **Tool:** Screenshot + annotations
- **Priority:** MEDIUM
- **Content:**
  - Login form (TenantId, Email, Password)
  - Submit button
  - Error message area
- **Estimated Size:** Half page
- **For:** Section 4.2.1 (UI Design)

### 21. UI Wireframe - Chat Interface
- **Type:** Wireframe/Mockup
- **Tool:** Screenshot + annotations
- **Priority:** HIGH
- **Content:**
  - Sidebar: Conversation list
  - Main area: Chat messages
  - Input area: Message box + send button
  - Header: User profile
  - Annotations for key features
- **Estimated Size:** Full page
- **For:** Section 4.2.1 (UI Design)

### 22. UI Wireframe - Document Management
- **Type:** Wireframe/Mockup
- **Tool:** Screenshot + annotations
- **Priority:** LOW
- **Content:**
  - Document list table
  - Upload button
  - Filter/search
  - Status indicators
- **Estimated Size:** Half page
- **For:** Section 4.2.1 (UI Design)

---

## CHAPTER 5 - GIẢI PHÁP VÀ ĐÓNG GÓP (Solutions and Contributions)

**Note:** Chapter 5 already completed. Only cross-reference diagrams from Chapter 4.

### 23. Dual-RAG Architecture Diagram
- **Type:** Architecture Diagram
- **Tool:** PlantUML or Mermaid
- **Priority:** HIGH (if not in Chapter 5 already)
- **Content:**
  - Query → Parallel search (Company Rules + Legal Base)
  - Scenario detection: BOTH/COMPANY_ONLY/LEGAL_ONLY/NONE
  - Context structuring with delimiters
  - System prompt selection based on scenario
  - LLM generation
  - Show tenant_id filtering at Qdrant level
- **Estimated Size:** Full page
- **For:** Section 5.2 (Dual-RAG Solution) - if not already in Chapter 5

### 24. Hierarchical Chunking Algorithm Flowchart
- **Type:** Flowchart
- **Tool:** PlantUML
- **Priority:** MEDIUM (if not in Chapter 5 already)
- **Content:**
  - Read .docx file
  - For each paragraph:
    - Match RegexHeading1 (Chương)
    - Match RegexHeading2 (Mục)
    - Match RegexHeading3 (Điều)
    - Extract content
  - Build chunk: {Heading1, Heading2, Heading3, Content, FullText}
  - Decision nodes for regex matches
- **Estimated Size:** Full page
- **For:** Section 5.3 (Hierarchical Chunking) - if not already in Chapter 5

---

## CHAPTER 6 - KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN (Conclusion and Future Work)

### 25. System Evolution Roadmap
- **Type:** Timeline/Roadmap Diagram
- **Tool:** PlantUML (Gantt chart) or Draw.io
- **Priority:** LOW
- **Content:**
  - Current features (completed)
  - Short-term (3-6 months): Security enhancements, Redis caching
  - Medium-term (6-12 months): Multi-language, Mobile app
  - Long-term (1-2 years): Advanced AI features, Enterprise SSO
- **Estimated Size:** Half page
- **For:** Section 6.2 (Future Development)

### 26. Comparison with Existing Systems (Bar Chart)
- **Type:** Bar Chart/Comparison Visualization
- **Tool:** LaTeX pgfplots or Excel → Image
- **Priority:** LOW
- **Content:**
  - Feature comparison scores (AIChat2025 vs competitors)
  - Categories: Multi-tenancy, RAG quality, Vietnamese support, Real-time, Customization
  - Bar heights represent capability level
- **Estimated Size:** Half page
- **For:** Section 6.1.2 (Comparison with similar systems)

---

## APPENDIX B - ĐẶC TẢ USE CASE CHI TIẾT (Detailed Use Case Specifications)

### 27. Activity Diagram - User Profile Management
- **Type:** UML Activity Diagram
- **Tool:** PlantUML
- **Priority:** LOW
- **Content:**
  - View profile
  - Edit profile fields
  - Change password
  - Upload avatar
  - Save changes
- **Estimated Size:** Half page
- **For:** Appendix B - Additional use cases

### 28. Activity Diagram - Tenant Configuration
- **Type:** UML Activity Diagram
- **Tool:** PlantUML
- **Priority:** LOW
- **Content:**
  - Tenant admin accesses config
  - Upload company documents
  - Configure prompt keywords
  - Manage users
  - Set permissions
- **Estimated Size:** Half page
- **For:** Appendix B - Additional use cases

---

## PLANTUML TEMPLATES TO CREATE (Priority Order)

1. **use_case_overview.puml** - System use case diagram
2. **system_architecture_context.puml** - C4 context
3. **system_architecture_container.puml** - C4 container
4. **multi_tenant_data_flow.puml** - Multi-tenant flow
5. **rag_pipeline_sequence.puml** - RAG processing sequence
6. **authentication_sequence.puml** - Auth flow
7. **document_embedding_sequence.puml** - Document processing
8. **database_er_diagram.puml** - Complete ER diagram
9. **deployment_diagram.puml** - Docker deployment
10. **class_diagram_auth.puml** - Authentication classes

---

## DIAGRAM CREATION SCHEDULE

### High Priority (Create First)
- System architecture diagrams (C4 context, container)
- RAG pipeline sequence
- Database ER diagram
- Deployment diagram
- Multi-tenant data flow

### Medium Priority
- Class diagrams (3 modules)
- Activity diagrams (main use cases)
- UI wireframes

### Low Priority (Nice to Have)
- Comparison charts
- Evolution roadmap
- Appendix diagrams

---

## TOOLS & RESOURCES

**PlantUML:**
- Download: https://plantuml.com/download
- Online editor: https://www.plantuml.com/plantuml/uml/
- VS Code extension: PlantUML by jebbs
- Syntax reference: https://plantuml.com/guide

**C4 PlantUML:**
- GitHub: https://github.com/plantuml-stdlib/C4-PlantUML
- Include in diagrams: `!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Container.puml`

**Export Formats:**
- PNG (for Word/LaTeX documents)
- SVG (for scalable vector graphics)
- PDF (for direct inclusion in LaTeX)

---

**END OF DIAGRAMS SPECIFICATION**
