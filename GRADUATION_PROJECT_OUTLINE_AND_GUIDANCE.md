# Graduation Project Outline & Guidance
## AIChat2025 - Multi-tenant RAG Legal Advisory System

---

## PROJECT STRUCTURE OVERVIEW

**Total Length**: 50-80 pages
**Chapters**: 6 main chapters + 2 appendices
**Core Innovation**: Multi-tenant RAG architecture with compliance verification for legal advisory

---

## CHAPTER 1: GIỚI THIỆU ĐỀ TÀI (Introduction) - 3-6 pages

### 1.1 Đặt vấn đề (Problem Statement)
**What to write:**
- Identify the urgent need for legal advisory systems in multi-tenant environments
- Emphasize the challenge of legal compliance verification across internal policies vs national laws
- Highlight the gap: existing systems don't provide real-time compliance checking with dual knowledge bases

**Key points from logic.md:**
- Legal document consultation requires contextual understanding of hierarchical structures
- Organizations need to verify internal policies against statutory requirements
- Current RAG systems use naive chunking that breaks semantic boundaries

### 1.2 Mục tiêu và phạm vi (Objectives and Scope)
**What to write:**
- Survey existing legal advisory systems and their limitations
- Define main functions: Multi-tenant chat, Document ingestion with hierarchical chunking, Dual-RAG compliance verification
- Scope: Focus on Vietnamese legal documents with tenant isolation

**Core features:**
- Multi-tenant document management
- Intelligent semantic search with metadata filtering
- Real-time compliance verification
- Asynchronous AI processing pipeline

### 1.3 Định hướng giải pháp (Solution Approach)
**What to write:**
- Approach: Microservices architecture + RAG with dual knowledge base
- Technology: .NET 9, Python AI workers, RabbitMQ, Qdrant vector DB
- Contribution: Hierarchical structural chunking + compliance-driven dual-RAG pattern

### 1.4 Bố cục đồ án (Document Structure)
Brief description of each subsequent chapter (paragraph form, no bullets)

---

## CHAPTER 2: KHẢO SÁT VÀ PHÂN TÍCH YÊU CẦU (Requirements Analysis) - 9-11 pages

### 2.1 Khảo sát hiện trạng (Current State Survey)
**What to write:**
- Compare existing solutions: ChatGPT, Claude, domain-specific legal AI tools
- Limitations: No tenant isolation, no compliance checking, naive document chunking
- Create comparison table showing feature gaps

### 2.2 Tổng quan chức năng (Functional Overview)

#### 2.2.1 Use Case Diagram (Overall)
**Actors:**
- System Admin (tenant management, global configuration)
- Tenant Admin (document upload, user management)
- End User (chat, document consultation)

**High-level use cases:**
- Manage Tenants
- Manage Documents
- Chat Consultation
- View History

#### 2.2.2 Decomposed Use Cases
Break down each high-level use case into specific actions

#### 2.2.3 Business Process
**Key workflow:**
Document Upload → Heading Standardization → Hierarchical Chunking → Vectorization → Storage with Tenant Metadata → Query → Dual-RAG Retrieval → LLM Inference → Compliance Verification → Response

### 2.3 Đặc tả chức năng (Detailed Specifications)
Select 4-7 most important use cases:
1. **Upload and Process Document**: Event flow, preconditions, postconditions
2. **Ask Legal Question**: Includes dual-RAG retrieval
3. **Receive Compliance Warning**: When policy violates statutory law
4. **Manage Tenant Isolation**: Security enforcement

### 2.4 Yêu cầu phi chức năng (Non-functional Requirements)
- Performance: LLM response < 30s, semantic search < 100ms
- Security: JWT-based auth, tenant isolation at all layers
- Scalability: Horizontal scaling of AI workers
- Reliability: Message queue for fault tolerance

---

## CHAPTER 3: CÔNG NGHỆ SỬ DỤNG (Technologies) - Max 10 pages

**Format**: For each technology, explain what problem it solves from Chapter 2, list alternatives, justify selection.

### Key Technologies:

**Backend Framework**: .NET 9
- Problem: Need robust microservices with strong typing
- Alternatives: Node.js, Java Spring Boot
- Reason: Performance, mature ecosystem, excellent EF Core ORM

**AI Framework**: Python with LangChain
- Problem: Need LLM integration and RAG orchestration
- Alternatives: JavaScript LangChain, Java AI frameworks
- Reason: Richest AI/ML library ecosystem, ONNX Runtime support

**Message Queue**: RabbitMQ
- Problem: Async processing of long-running LLM operations
- Alternatives: Kafka, Redis Streams, Azure Service Bus
- Reason: Lightweight, reliable, built-in retry mechanisms

**Vector Database**: Qdrant
- Problem: Fast semantic search with metadata filtering for tenant isolation
- Alternatives: Pinecone, Weaviate, Milvus
- Reason: Open-source, excellent filter performance, easy deployment

**API Gateway**: YARP (Yet Another Reverse Proxy)
- Problem: Unified authentication, routing, API documentation aggregation
- Alternatives: Ocelot, Kong, Nginx
- Reason: Native .NET integration, dynamic configuration

**Embedding Model**: Vietnamese Legal Text Embedding (ONNX optimized)
- Problem: Semantic understanding of Vietnamese legal terminology
- Reason: Domain-specific, 2-3x faster inference with ONNX

---

## CHAPTER 4: THIẾT KẾ, TRIỂN KHAI VÀ ĐÁNH GIÁ (Design & Implementation)

### 4.1 Thiết kế kiến trúc (Architecture Design)

#### 4.1.1 Software Architecture Selection
**Architecture**: Microservices + Event-Driven + BFF Pattern

**Services:**
```
├── API Gateway (YARP)
├── Account Service (.NET)
├── Chat Service (.NET + SignalR)
├── Document Service (.NET)
├── Embedding Service (Python)
└── Chat Processor (Python)
```

#### 4.1.2 Overall Design - UML Package Diagram
**Layers:**
- Presentation: API Gateway, Client Apps
- Application: Service APIs
- Domain: Business logic, entities
- Infrastructure: Database, Message Queue, Vector DB

### 4.2 Thiết kế chi tiết (Detailed Design)

#### 4.2.1 User Interface Design
- Web application (React/Vue)
- Chat interface with real-time updates via SignalR
- Document upload with progress tracking
- Admin dashboard for tenant management

#### 4.2.2 Class Design
**Key classes:**
- `CurrentUserProvider`: Extract tenant context from JWT
- `DocumentChunker`: Hierarchical chunking logic
- `DualRagRetriever`: Parallel query to tenant + shared knowledge base
- `ComplianceAnalyzer`: Detect policy-law conflicts

Include 2-3 sequence diagrams for critical use cases:
1. Document ingestion pipeline
2. Chat request with dual-RAG retrieval
3. Background job processing

#### 4.2.3 Database Design
**SQL Database (User/Document metadata):**
- Tenants, Users, Documents, Conversations, Messages
- EF Core with tenant filtering interceptor

**Vector Database (Qdrant):**
- Collections with tenant_id metadata
- Payload: text, document_id, heading1, heading2, heading3

### 4.3 Xây dựng ứng dụng (Application Development)

#### 4.3.1 Tools & Libraries
Create table:
| Purpose | Tool | Version |
|---------|------|---------|
| Backend IDE | Visual Studio 2024 | 17.x |
| Python IDE | VS Code | Latest |
| Vector DB | Qdrant | 1.7.x |
| Message Queue | RabbitMQ | 3.12 |
| Embedding | ONNX Runtime | 1.16 |

#### 4.3.2 Results Achieved
- Lines of code: ~15,000 (C#) + ~3,000 (Python)
- Number of classes: 120+
- Number of packages: 15+
- Deployment artifacts: 6 Docker containers

#### 4.3.3 Main Features Demo
Screenshots with annotations for:
- Document upload with heading detection
- Chat interface showing compliance warning
- Admin tenant management
- Real-time response streaming

### 4.4 Kiểm thử (Testing)
Test 2-3 critical functions:
1. **Tenant isolation testing**: Verify cross-tenant data leakage prevention
2. **Dual-RAG retrieval testing**: Confirm both knowledge bases are queried
3. **Compliance detection testing**: Test warning generation for policy violations

Summary: X test cases, Y% pass rate

### 4.5 Triển khai (Deployment)
**Model**: Docker Compose for development, Kubernetes for production
**Infrastructure**:
- 6 containers: Gateway, 3 .NET services, 2 Python services
- Qdrant vector DB
- RabbitMQ message broker
- SQL Server database
- MinIO object storage

---

## CHAPTER 5: CÁC GIẢI PHÁP VÀ ĐÓNG GÓP NỔI BẬT (Solutions & Contributions) - Min 5 pages

**Critical**: This chapter determines your evaluation score. Show creativity, analysis, and problem-solving.

### 5.1 Hierarchical Data Modeling and Context-Aware Chunking

#### Problem
- **Loss of Legal Context**: In the Vietnamese legal system, a Decree (*Nghị định*) is meaningless without reference to the Law (*Luật*) it guides. Standard RAG retrieval treats all documents as isolated islands.
- **Ambiguity in Vector Search**: A chunk from a Decree might mention "Fine of 5,000,000 VND," but without knowing which Law it belongs to, the LLM cannot apply it correctly.
- **Flat Data Structures**: Naive database designs cannot efficiently represent the recursive "Law → Decree → Circular" relationship.

#### Solution
**1. Self-Referencing Entity Model (The "Chain of Validity"):**
- Implemented a self-referencing `Documents` table strategy.
- Logic: `Type 1` (Law) acts as the root. `Type 2` (Decree) contains a `FatherDocumentId` pointing to the Law.
- This creates a hard-linked "skeleton" of legal validity before any AI processing occurs.

**2. Metadata Enrichment at Ingestion:**
- **Dynamic Parent Lookup**: During the chunking process for a Decree, the system performs a reverse lookup to fetch the `DocumentName` of the parent Law.
- **Context Injection**: The parent document's name is injected into the Qdrant payload (`father_doc_name`) and the embedding context.
- **Structure**:
  ```csharp
  // Data pushed to Qdrant
  {
      "content": "Article 5: Penalties for violations...",
      "metadata": {
          "doc_type": "Decree",
          "father_doc_name": "Law on Cyber Security 2018", // Crucial context added
          "heading1": "Chapter II",
          "heading2": "Section 1"
      }
  }
  ```

### 5.2 Compliance-Driven Dual-RAG Architecture

#### Problem
- Users need both: "What does my company policy say?" AND "Is it legal?"
- Single knowledge base insufficient for compliance checking
- Post-hoc verification requires multiple LLM calls

#### Solution
**Parallel Dual-Source Retrieval:**
```
1. Generate query embedding
2. Execute parallel searches:
   - Search 1: Filter tenant_id = current_tenant (company policies)
   - Search 2: Filter tenant_id = 1 (national legal framework)
3. Retrieve top-3 from each source
4. Structure context with source labels:
   [Internal Company Regulations]
   ... company policy chunks ...
   [Legal Framework Documents]
   ... statutory law chunks ...
5. LLM reasoning template:
   - Answer based on company policy
   - Cross-reference with statutory requirements
   - Flag violations with warning
```

**Qdrant Filter Logic:**
```python
search_query = {
    "filter": {
        "should": [
            {"match": {"tenant_id": current_tenant_id}},
            {"match": {"tenant_id": 1}}  # Shared legal framework
        ]
    },
    "limit": 6
}
```

#### Results
- Single-pass compliance verification
- Users receive warnings when policies violate laws
- Reduces legal risk for organizations

### 5.3 Infrastructure-Level Tenant Context Propagation

#### Problem
- Microservices require tenant context at every layer
- Manual tenant passing error-prone
- Background jobs have no HTTP context

#### Solution
**JWT-Based Tenant Continuity:**
```
1. Account Service generates JWT with claims: user_id, tenant_id, username, is_admin
2. All services share identical JWT configuration
3. ICurrentUserProvider abstraction extracts tenant from HTTP context
4. Hybrid provider for background jobs: try HTTP context → fallback to thread-local storage
5. Message payloads carry tenant_id for impersonation
6. Vector queries automatically filter by extracted tenant_id
```

**Defense-in-Depth:**
- Layer 1: Gateway validates JWT
- Layer 2: Services independently validate JWT
- Layer 3: EF Core query filter adds tenant WHERE clause
- Layer 4: Database interceptor stamps tenant_id on inserts
- Layer 5: Vector DB metadata filter enforces isolation

#### Results
- Zero cross-tenant data leakage in testing
- Compile-time safety via dependency injection
- Seamless tenant context in sync and async operations

### 5.4 Asynchronous Distributed AI Processing Pipeline

#### Problem
- LLM inference takes 30+ seconds
- Long HTTP timeouts hold server resources
- Need scalable AI processing

#### Solution
**Event-Driven Architecture:**
```
User → Gateway → Chat Service → [Persist Message] → RabbitMQ → Chat Processor → [LLM Inference] → RabbitMQ → Chat Service → SignalR → User
```

**Key Design Decisions:**
- Chat Service returns 202 Accepted immediately
- RabbitMQ decouples request from processing
- Prefetch=1 prevents worker starvation
- SignalR provides real-time response delivery
- Hangfire for document processing background jobs

#### Results
- Horizontal scaling: Add more Chat Processor instances
- Fault tolerance: Message requeue on failure
- User experience: Non-blocking interface

---

## CHAPTER 6: KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN (Conclusion) - 3-5 pages

### 6.1 Kết luận (Conclusion)
**Comparison with existing solutions:**
- vs. ChatGPT: AIChat2025 provides tenant isolation + compliance checking
- vs. Domain legal tools: Better RAG with hierarchical chunking

**Achievements:**
- Multi-tenant RAG system with strict isolation
- Dual-RAG compliance verification
- Hierarchical semantic chunking for legal documents
- Scalable async architecture

**Lessons learned:**
- Importance of semantic boundaries in chunking
- Defense-in-depth for tenant security
- Event-driven architecture for long-running operations

### 6.2 Hướng phát triển (Future Work)
**Completion tasks:**
- Add OCR for scanned legal documents
- Implement refresh token rotation
- Enhanced audit logging

**Improvement directions:**
- Fine-tune embedding model on tenant-specific vocabulary
- Implement learned relevance reranking
- Add conversational memory with context window management
- Support multi-language (English legal documents)
- Build closed-loop learning from user corrections

---

## APPENDIX A: FORMATTING REQUIREMENTS (Phu_luc_A.tex)

### Document Standards
- Font: Times New Roman, 13pt
- Margins: Top 2cm, Bottom 2cm, Left 3.5cm, Right 2.5cm
- Line spacing: 1.5 (onehalfspacing)
- Paragraph indent: 15pt
- ISO 7144:1986 compliant

### Critical Rules
1. **NO plagiarism** - cite all sources
2. **NO bullet points** - write full paragraphs
3. **All figures/tables must be referenced** in text
4. **Consistent formatting** - use template styles
5. **Scientific writing** - avoid subjective terms like "amazing", "wonderful"

### Chapter Structure
- Each chapter needs: Opening paragraph + Content + Closing paragraph
- Opening: Link from previous chapter + chapter overview
- Closing: Summarize main points + link to next chapter

### Citations
- Use IEEE style: \cite{reference_key}
- Only cited references appear in bibliography
- Prefer academic sources over Wikipedia

### Figures & Tables
- Figures: Caption below image
- Tables: Caption above table
- Both: Must be referenced and explained in text

### Binding Requirements
- Front cover: Glossy paper
- Spine info: Semester - Major - Name - Student ID
- Use thermal glue (not tape or staples)

---

## APPENDIX B: USE CASE SPECIFICATIONS

Detailed specifications for 4-7 critical use cases with:
- Name
- Event flow (main + alternate)
- Preconditions
- Postconditions
- Activity diagrams (if complex)

---

## KEY INTEGRATION POINTS (Logic.md → LaTeX Structure)

### Map Technical Content to Chapters:

**Logic.md Section 1 (High-Level Architecture)** → Chapter 4.1
**Logic.md Section 2 (Security)** → Chapter 4.1 + Chapter 5.3
**Logic.md Section 3 (AI Reasoning)** → Chapter 5.1 + 5.2
**Logic.md Section 4 (Evaluation)** → Chapter 4.4 + 5.5
**Logic.md Section 5 (Robustness/Scalability)** → Chapter 3 (tech selection) + 4.5 (deployment)

### Writing Strategy:

1. **Chapter 1**: Brief mention of innovations, defer details to Chapter 5
2. **Chapter 2**: Focus on requirements that led to design decisions
3. **Chapter 3**: Justify tech stack based on requirements
4. **Chapter 4**: Show what you built, brief on how
5. **Chapter 5**: Deep dive on HOW - your creative solutions
6. **Chapter 6**: Reflect on achievements and future vision

### Cross-References:
Use liberally: "The hierarchical chunking strategy will be detailed in Section 5.1"

---

## FINAL CHECKLIST

- [ ] All figures/tables numbered and referenced
- [ ] All citations in IEEE format
- [ ] No bullet points in main content (use paragraphs)
- [ ] Each chapter has opening/closing paragraphs
- [ ] Chapter 5 has minimum 5 pages of original contribution
- [ ] Use cases properly specified with event flows
- [ ] UML diagrams follow standards
- [ ] Code examples use proper formatting
- [ ] Tenant isolation verified at all layers
- [ ] Evaluation metrics included
- [ ] Future work is concrete, not vague
- [ ] No plagiarism - all sources cited
- [ ] Consistent terminology throughout
- [ ] Technical accuracy verified

---

**Page Count Targets:**
- Chapter 1: 3-6 pages
- Chapter 2: 9-11 pages
- Chapter 3: ≤10 pages
- Chapter 4: 20-25 pages
- Chapter 5: ≥5 pages (no limit)
- Chapter 6: 3-5 pages
- **Total: 50-80 pages**
