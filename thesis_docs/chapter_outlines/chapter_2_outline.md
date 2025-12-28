# CHƯƠNG 2: KHẢO SÁT VÀ PHÂN TÍCH

**Mục đích:** Tổng quan lý thuyết, khảo sát các nghiên cứu liên quan, phân tích yêu cầu hệ thống

**Số trang ước tính:** 15-18 trang

---

## 2.1. Tổng quan về các khái niệm và công nghệ

**Nội dung chính:**

### 2.1.1. Large Language Model (LLM) - Mô hình ngôn ngữ lớn

**Định nghĩa:**
- Mô hình neural network được huấn luyện trên hàng tỷ từ để hiểu và sinh văn bản tự nhiên
- Dựa trên kiến trúc Transformer (Vaswani et al., 2017)
- Ví dụ: GPT-4, Claude, Gemini, LLaMA

**Kiến trúc Transformer:**
- Self-attention mechanism
- Multi-head attention
- Positional encoding
- **Sơ đồ:** `diagrams_to_create.md` → Diagram 2.1 (Transformer Architecture)

**Ưu điểm:**
- Hiểu ngữ cảnh, sinh văn bản tự nhiên
- Zero-shot, few-shot learning
- Đa nhiệm (multi-task)

**Nhược điểm:**
- **Hallucination:** Sinh thông tin sai lệch, không có trong dữ liệu training
- **Knowledge cutoff:** Không biết thông tin sau ngày cutoff
- **Domain-specific:** Thiếu kiến thức chuyên sâu về lĩnh vực cụ thể (ví dụ: pháp luật Việt Nam)

**LLM cho tiếng Việt:**
- **Vistral** (ontocord/vistral): LLaMA fine-tuned cho tiếng Việt
- **PhoGPT** (VinAI): GPT cho tiếng Việt
- **Gemma-Vietnamese**: Google Gemma fine-tuned

### 2.1.2. Retrieval-Augmented Generation (RAG)

**Định nghĩa:**
- Kỹ thuật kết hợp **retrieval (tìm kiếm)** + **generation (sinh văn bản)**
- Truy xuất thông tin từ cơ sở tri thức bên ngoài → Cung cấp cho LLM làm context → LLM sinh câu trả lời dựa trên context

**Kiến trúc RAG cơ bản:**
```
User Query
    ↓
[1] Embedding (Vector hóa câu hỏi)
    ↓
[2] Vector Search (Tìm kiếm trong cơ sở tri thức)
    ↓
[3] Context Retrieval (Lấy top-k documents)
    ↓
[4] Prompt Construction (Kết hợp query + context)
    ↓
[5] LLM Generation (Sinh câu trả lời)
    ↓
Response
```

**Sơ đồ:** `diagrams_to_create.md` → Diagram 2.2 (RAG Pipeline Concept)

**Ưu điểm RAG:**
- ✅ Giảm hallucination (dựa trên dữ liệu thực)
- ✅ Cập nhật kiến thức mà không cần retrain LLM
- ✅ Trích dẫn nguồn (traceability)
- ✅ Domain-specific knowledge

**So sánh RAG vs Fine-tuning vs Prompt Engineering:**

| Tiêu chí | RAG | Fine-tuning | Prompt Engineering |
|----------|-----|-------------|--------------------|
| Chi phí | Thấp | Cao (cần GPU) | Rất thấp |
| Thời gian | Nhanh | Chậm (vài giờ - vài ngày) | Rất nhanh |
| Cập nhật dữ liệu | Dễ (chỉ cần thêm vào vector DB) | Khó (cần retrain) | Không thay đổi model |
| Tính chính xác | Cao (với dữ liệu tốt) | Rất cao | Trung bình |
| Trích dẫn nguồn | ✅ Có | ❌ Không | ❌ Không |
| Use case | Chatbot, Q&A, tư vấn | Task-specific (classification, NER) | General-purpose chat |

**Kết luận:** RAG phù hợp nhất cho bài toán tư vấn pháp lý

### 2.1.3. Vector Embedding và Vector Database

**Vector Embedding:**
- Chuyển đổi text thành vector số (ví dụ: 768 chiều)
- Mô hình: Sentence Transformers, BERT, E5, BGE
- **Đặc biệt:** `truro7/vn-law-embedding` - mô hình chuyên biệt cho văn bản pháp luật Việt Nam

**Ví dụ:**
```
Text: "Thời gian thử việc là bao lâu?"
↓ (Embedding model)
Vector: [0.23, -0.45, 0.67, ..., 0.12] (768 dimensions)
```

**Semantic Search:**
- Tìm kiếm dựa trên **ý nghĩa** thay vì từ khóa
- Sử dụng **cosine similarity** giữa các vector

**Ví dụ:**
```
Query: "Nhân viên có được nghỉ thai sản bao lâu?"
Kết quả gần nhất:
  1. "Thời gian nghỉ sinh là 6 tháng" (similarity: 0.92)
  2. "Chế độ thai sản theo Điều 138" (similarity: 0.88)
  3. "Phụ cấp bảo hiểm thai sản" (similarity: 0.75)

Keyword search sẽ KHÔNG tìm thấy (không có từ "thai sản" trong query)
```

**Vector Database:**
- Cơ sở dữ liệu chuyên dụng lưu trữ và tìm kiếm vector
- Thuật toán: HNSW (Hierarchical Navigable Small World)
- Ví dụ: Qdrant, Milvus, Weaviate, Pinecone, Chroma

**So sánh:**

| Vector DB | Ưu điểm | Nhược điểm | License |
|-----------|---------|------------|---------|
| **Qdrant** | Nhanh, open-source, self-hosted, Rust-based | Cộng đồng nhỏ hơn | Apache 2.0 |
| Milvus | Phổ biến, scale tốt | Phức tạp, tốn tài nguyên | Apache 2.0 |
| Weaviate | Tích hợp tốt, GraphQL | Chậm hơn | BSD-3 |
| Pinecone | Managed service, dễ dùng | Không self-hosted, tốn tiền | Proprietary |

**Lựa chọn:** Qdrant (phù hợp self-hosted, open-source, hiệu năng cao)

### 2.1.4. Multi-tenancy (Đa thuê bao)

**Định nghĩa:**
- Một hệ thống phục vụ nhiều khách hàng (tenant) trên cùng một cơ sở hạ tầng
- Mỗi tenant có dữ liệu và cấu hình riêng biệt, hoàn toàn cô lập

**3 mô hình Multi-tenant:**

**1. Shared Database, Shared Schema (Row-level isolation)**
```
Tenants: A, B, C
Database: 1 database, 1 schema, nhiều tables
Isolation: WHERE TenantId = 'A'

Ưu điểm: Chi phí thấp, dễ quản lý
Nhược điểm: Rủi ro bảo mật (lỗi code → leak data)
Use case: SaaS nhỏ và vừa
```

**2. Shared Database, Separate Schema**
```
Tenants: A, B, C
Database: 1 database, nhiều schemas (schema_A, schema_B, schema_C)

Ưu điểm: Cô lập tốt hơn, vẫn tiết kiệm
Nhược điểm: Phức tạp hơn, migration khó
Use case: Enterprise SaaS
```

**3. Separate Database**
```
Tenants: A, B, C
Databases: db_A, db_B, db_C

Ưu điểm: Cô lập tuyệt đối, an toàn nhất
Nhược điểm: Chi phí cao, khó quản lý
Use case: Regulated industries (healthcare, finance)
```

**Sơ đồ:** `diagrams_to_create.md` → Diagram 2.3 (Multi-tenant Patterns Comparison)

**Lựa chọn cho AIChat2025:** Shared Database, Shared Schema (Row-level isolation)
- **Lý do:**
  - Chi phí thấp (phù hợp thesis project)
  - Dễ deploy (1 container SQL Server)
  - Đủ an toàn với EF Core interceptors

**Chi tiết triển khai:** Xem Mục 5.2 (kiến trúc multi-tenant)

### 2.1.5. Microservices Architecture

**Định nghĩa:**
- Kiến trúc phần mềm chia ứng dụng thành nhiều services nhỏ, độc lập
- Mỗi service có database riêng, giao tiếp qua API/message queue

**Ưu điểm:**
- Scalability: Scale từng service riêng
- Technology diversity: Mỗi service có thể dùng tech khác nhau (.NET + Python)
- Fault isolation: Lỗi một service không ảnh hưởng toàn hệ thống
- Team autonomy: Mỗi team phát triển riêng

**Nhược điểm:**
- Phức tạp hơn monolith
- Distributed transactions khó
- Debugging khó hơn

**Áp dụng trong AIChat2025:**
- 7 microservices .NET + 2 AI workers Python
- Giao tiếp: RabbitMQ (async), HTTP (sync), SignalR (real-time)

**Chi tiết:** Xem Mục 5.1 (kiến trúc hệ thống tổng quan)

---

## 2.2. Các nghiên cứu và hệ thống liên quan

**Nội dung chính:**

### 2.2.1. Các nghiên cứu về RAG

**1. RAG gốc (Lewis et al., 2020)**
- Paper: "Retrieval-Augmented Generation for Knowledge-Intensive NLP Tasks"
- Đề xuất: Kết hợp DPR (Dense Passage Retrieval) + BART (sequence-to-sequence model)
- Dataset: Natural Questions, TriviaQA
- **Hạn chế:** Tiếng Anh, không specialized cho legal domain

**2. Self-RAG (Asai et al., 2023)**
- Cải tiến: LLM tự đánh giá và quyết định khi nào cần retrieval
- **Reflection tokens:** [Retrieve], [IsRel], [IsSup], [IsUse]
- **Ý tưởng:** Áp dụng cho so sánh quy định công ty vs luật (phát hiện mâu thuẫn)

**3. RAG for Legal Domain**
- **LegalBench** (Guha et al., 2023): Benchmark cho legal reasoning
- **CaseHOLD** (Zheng et al., 2021): Legal citation prediction
- **Hạn chế:** Hầu hết cho common law (Mỹ, Anh), ít cho civil law (Việt Nam)

### 2.2.2. Chatbot pháp lý hiện có

**1. DoNotPay (Mỹ)**
- Chatbot AI cho tư vấn pháp lý tiêu dùng
- **Hạn chế:** Tiếng Anh, không open-source, không multi-tenant

**2. LawBot (Trung Quốc)**
- Chatbot cho pháp luật Trung Quốc
- **Hạn chế:** Tiếng Trung, không public

**3. Chatbot pháp lý Việt Nam (nghiên cứu học thuật)**
- Một số đề tài sinh viên về chatbot luật
- **Hạn chế:**
  - Dùng rule-based hoặc intent classification (không phải RAG)
  - Không hỗ trợ quy định nội bộ công ty
  - Không multi-tenant
  - Không có trích dẫn chính xác

**Kết luận:** Chưa có hệ thống nào kết hợp RAG + multi-tenant + dual knowledge base (company + legal) cho Việt Nam

### 2.2.3. Hệ thống quản lý tri thức doanh nghiệp

**1. Confluence + Slack (Atlassian)**
- Quản lý tài liệu + chat
- **Hạn chế:** Không có AI Q&A, phải tìm kiếm thủ công

**2. Notion AI**
- Tích hợp AI vào knowledge base
- **Hạn chế:** General-purpose, không specialized cho legal, không multi-tenant tách biệt

**3. Microsoft Copilot + SharePoint**
- RAG trên tài liệu nội bộ
- **Hạn chế:** Đắt, không customize cho legal domain, không so sánh với luật nhà nước

**Khoảng trống (Gap):** Hệ thống RAG đa tenant cho tư vấn pháp lý nội bộ Việt Nam

---

## 2.3. Phân tích yêu cầu hệ thống

**Nội dung chính:**

### 2.3.1. Yêu cầu chức năng (Functional Requirements)

**FR1. Quản lý người dùng và xác thực**
- FR1.1: Đăng ký tài khoản (email, mật khẩu, tên, công ty)
- FR1.2: Đăng nhập với email/password
- FR1.3: Xác thực JWT cho web và mobile
- FR1.4: Phân quyền (admin, user)

**FR2. Quản lý tenant (công ty)**
- FR2.1: Tạo mới tenant (admin system)
- FR2.2: Liệt kê tenants
- FR2.3: Cô lập dữ liệu giữa các tenant (row-level security)

**FR3. Quản lý tài liệu**
- FR3.1: Upload tài liệu .docx
- FR3.2: Phân tích cấu trúc (Chương/Mục/Điều/Khoản)
- FR3.3: Hierarchical semantic chunking
- FR3.4: Background vectorization (Hangfire job)
- FR3.5: Lưu trữ file vào MinIO
- FR3.6: Lưu trữ vectors vào Qdrant (với metadata: document_name, heading1, heading2)
- FR3.7: Liệt kê, xóa, chỉnh sửa tài liệu

**FR4. Chat và RAG**
- FR4.1: Tạo conversation mới
- FR4.2: Gửi message qua SignalR hoặc HTTP API
- FR4.3: Dual-RAG search:
  - Tìm kiếm trong quy định công ty (tenant-specific)
  - Tìm kiếm trong luật nhà nước (tenant_id=1, shared)
- FR4.4: Prompt engineering cho 3 scenario:
  - COMPANY_ONLY: Chỉ dựa quy định công ty
  - LEGAL_ONLY: Chỉ dựa luật nhà nước
  - COMPARISON: So sánh quy định công ty vs luật
- FR4.5: LLM generation với Vistral (Ollama)
- FR4.6: Trả lời với trích dẫn chính xác (document_name, heading)
- FR4.7: Real-time response qua SignalR
- FR4.8: Lưu lịch sử chat vào database

**FR5. Quản lý prompt config**
- FR5.1: Tạo, chỉnh sửa, xóa prompt template
- FR5.2: Customize system prompt cho từng tenant

**FR6. Admin dashboard**
- FR6.1: Hangfire dashboard (background jobs)
- FR6.2: Swagger API documentation
- FR6.3: Quản lý tài khoản (CRUD)

### 2.3.2. Yêu cầu phi chức năng (Non-Functional Requirements)

**NFR1. Performance (Hiệu năng)**
- NFR1.1: API response time < 2 giây (excluding LLM generation)
- NFR1.2: RAG pipeline latency < 10 giây (end-to-end)
- NFR1.3: Vector search < 500ms (top-5 results)
- NFR1.4: Embedding speed: 100+ chunks/second

**NFR2. Scalability (Khả năng mở rộng)**
- NFR2.1: Hỗ trợ 10+ tenants đồng thời
- NFR2.2: Hỗ trợ 100+ concurrent users
- NFR2.3: Vector database có thể chứa 100,000+ chunks

**NFR3. Security (Bảo mật)**
- NFR3.1: Mật khẩu mã hóa BCrypt
- NFR3.2: JWT token với expiry (24 giờ)
- NFR3.3: Row-level security cho multi-tenant (TenantId filtering)
- NFR3.4: HTTPS cho production (not in dev)
- NFR3.5: CORS policy

**NFR4. Usability (Tính dễ sử dụng)**
- NFR4.1: Giao diện đơn giản, trực quan (Bootstrap 5)
- NFR4.2: Real-time feedback (SignalR)
- NFR4.3: Error messages rõ ràng

**NFR5. Reliability (Độ tin cậy)**
- NFR5.1: RabbitMQ retry mechanism (MassTransit)
- NFR5.2: Database connection pooling
- NFR5.3: Hangfire job retry (3 lần)

**NFR6. Maintainability (Tính bảo trì)**
- NFR6.1: Microservices architecture (separation of concerns)
- NFR6.2: Repository pattern + Specification pattern
- NFR6.3: Dependency injection
- NFR6.4: Structured logging (Serilog)

**NFR7. Portability (Tính di động)**
- NFR7.1: Docker Compose deployment
- NFR7.2: Cross-platform (.NET 9, Python 3.11+)

### 2.3.3. Use Cases chính

**Use Case 1: Nhân viên hỏi về quy định công ty**
```
Actor: Nhân viên
Precondition: Đã đăng nhập
Main Flow:
  1. Nhân viên nhập câu hỏi: "Công ty có bao nhiêu ngày phép năm?"
  2. Hệ thống gửi qua SignalR
  3. ChatService publish RabbitMQ event
  4. ChatProcessor nhận event
  5. Embedding câu hỏi
  6. Search trong Qdrant (collection tenant-specific)
  7. Retrieve top-5 chunks
  8. Prompt LLM với context từ quy định công ty
  9. LLM trả lời: "Theo Điều 15 Nội quy lao động, nhân viên có 12 ngày phép năm."
  10. Response qua RabbitMQ → ChatService → SignalR → Frontend
Postcondition: Lịch sử chat được lưu
```

**Use Case 2: HR upload tài liệu mới**
```
Actor: HR/Admin
Precondition: Đã đăng nhập, có quyền admin
Main Flow:
  1. HR chọn file .docx (Nội quy lao động 2024)
  2. Upload qua DocumentService API
  3. DocumentService lưu file vào MinIO
  4. Tạo record trong database
  5. Trigger Hangfire job (VectorizeBackgroundJob)
  6. Background job:
     a. Parse .docx (DocumentFormat.OpenXml)
     b. Phân tích cấu trúc (Chương/Điều/Khoản)
     c. Hierarchical chunking (preserve hierarchy)
     d. Gọi EmbeddingService API (batch embedding)
     e. Lưu vectors vào Qdrant với metadata
  7. Job hoàn thành, update status
Postcondition: Tài liệu có thể tìm kiếm trong chat
```

**Use Case 3: So sánh quy định công ty với luật nhà nước**
```
Actor: HR hoặc nhân viên
Precondition: Đã upload cả quy định công ty và luật lao động
Main Flow:
  1. User hỏi: "Thời gian thử việc của công ty có hợp pháp không?"
  2. Hệ thống nhận diện intent: COMPARISON
  3. Dual-RAG:
     a. Search trong company rules (tenant-specific)
        → Tìm thấy: "Thời gian thử việc: 90 ngày"
     b. Search trong legal base (tenant_id=1)
        → Tìm thấy: "Điều 24 Bộ luật Lao động: Thử việc tối đa 60 ngày"
  4. Prompt LLM với cả 2 context + system prompt COMPARISON
  5. LLM trả lời:
     "Quy định công ty về thử việc 90 ngày KHÔNG HỢP PHÁP.
      - Quy định công ty: 90 ngày (Điều X Nội quy)
      - Luật nhà nước: Tối đa 60 ngày (Điều 24 Bộ luật Lao động 2019)
      → Khuyến nghị: Điều chỉnh quy định công ty về 60 ngày để tuân thủ pháp luật."
Postcondition: HR được cảnh báo về mâu thuẫn
```

**Use Case Diagram:** `diagrams_to_create.md` → Diagram 1.1 (Use Case Overview)
**Activity Diagram:** Appendix B

### 2.3.4. Ràng buộc hệ thống (Constraints)

**1. Công nghệ:**
- Backend: .NET 9 (mới nhất, stable)
- Python: 3.11+ (cho AI workers)
- Database: SQL Server 2022 (free developer edition)
- Deployment: Docker Compose (local, không cloud)

**2. Thời gian:**
- Thời gian phát triển: 4 tháng (1 học kỳ)
- Không có thời gian cho unit tests đầy đủ

**3. Ngân sách:**
- Zero budget (không có tiền thuê cloud, API)
- Sử dụng open-source, self-hosted
- Ollama local (không gọi OpenAI API)

**4. Dữ liệu:**
- Chỉ tập trung vào pháp luật lao động
- Dữ liệu demo (không có dữ liệu thực từ doanh nghiệp)

**5. Phạm vi:**
- Chỉ phát triển web app (không có mobile app)
- Không triển khai production (chỉ demo)

---

## 2.4. Phân tích lựa chọn công nghệ

**Nội dung chính:**

### 2.4.1. Lựa chọn LLM cho tiếng Việt

**So sánh:**

| LLM | Ưu điểm | Nhược điểm | Lựa chọn |
|-----|---------|------------|----------|
| **Vistral** (ontocord/vistral:latest) | - Vietnamese-finetuned<br>- Chạy local (Ollama)<br>- Miễn phí<br>- 7B params (nhẹ) | - Chất lượng không bằng GPT-4<br>- Cần GPU/RAM | ✅ **CHỌN** |
| PhoGPT (VinAI) | - Vietnamese-specific<br>- Research-backed | - Khó deploy<br>- Không có Ollama support | ❌ |
| GPT-4 / Claude (OpenAI/Anthropic) | - Chất lượng cao nhất<br>- Hiểu tiếng Việt tốt | - Tốn tiền ($$$)<br>- Phụ thuộc API<br>- Latency cao | ❌ |
| Gemma-Vietnamese | - Google-backed<br>- Multilingual | - Mới, chưa stable<br>- Ít tài liệu | ❌ |

**Kết luận:** Vistral (Ollama) - phù hợp cho thesis (miễn phí, self-hosted, đủ tốt)

### 2.4.2. Lựa chọn Embedding Model

**So sánh:**

| Model | Dimensions | Ưu điểm | Nhược điểm | Lựa chọn |
|-------|------------|---------|------------|----------|
| **vn-law-embedding** (truro7) | 768 | - Specialized cho pháp luật VN<br>- Fine-tuned trên legal corpus | - Cộng đồng nhỏ<br>- Ít tài liệu | ✅ **CHỌN** |
| PhoBERT | 768 | - Vietnamese BERT<br>- Research-backed | - General-purpose<br>- Không specialized | ❌ |
| multilingual-e5-large | 1024 | - Multilingual<br>- Hiệu năng cao | - Không specialized cho VN<br>- Nặng hơn | ❌ |

**Kết luận:** vn-law-embedding - specialized cho legal domain Việt Nam

### 2.4.3. Lựa chọn Vector Database

**Đã phân tích ở mục 2.1.3**
**Kết luận:** Qdrant

### 2.4.4. Lựa chọn Message Queue

**So sánh:**

| Message Queue | Ưu điểm | Nhược điểm | Lựa chọn |
|---------------|---------|------------|----------|
| **RabbitMQ** | - Phổ biến<br>- Tích hợp tốt MassTransit<br>- Management UI | - Không phân tán (single node) | ✅ **CHỌN** |
| Apache Kafka | - High throughput<br>- Distributed | - Overkill cho thesis<br>- Phức tạp setup | ❌ |
| Redis Pub/Sub | - Đơn giản<br>- Nhanh | - Không persistent<br>- Không guaranteed delivery | ❌ |

**Kết luận:** RabbitMQ - phù hợp microservices, tích hợp MassTransit

---

## 2.5. Tổng kết chương

**Nội dung chính:**

### Những điểm chính đã trình bày:
1. ✅ Giải thích chi tiết RAG, LLM, Vector DB, Multi-tenancy, Microservices
2. ✅ Khảo sát các nghiên cứu liên quan → Xác định gap
3. ✅ Phân tích yêu cầu chức năng và phi chức năng
4. ✅ Đề xuất use cases chính
5. ✅ Lựa chọn công nghệ phù hợp (có so sánh, có lý do)

### Khoảng trống nghiên cứu (Research Gap):
- Chưa có hệ thống RAG đa tenant cho tư vấn pháp lý nội bộ Việt Nam
- Chưa có giải pháp kết hợp dual knowledge base (company + legal)
- Chưa có hierarchical chunking cho văn bản pháp luật Việt Nam

### Chuyển tiếp sang Chương 3:
- Chương 2 đã xác định **yêu cầu** và **lựa chọn công nghệ**
- Chương 3 sẽ trình bày chi tiết **các công nghệ được sử dụng**

---

## TÀI LIỆU THAM KHẢO CHO CHƯƠNG 2

### RAG và LLM
1. Lewis et al. (2020) - "Retrieval-Augmented Generation for Knowledge-Intensive NLP Tasks"
2. Asai et al. (2023) - "Self-RAG: Learning to Retrieve, Generate, and Critique through Self-Reflection"
3. Vaswani et al. (2017) - "Attention is All You Need" (Transformer paper)

### Legal AI
4. Guha et al. (2023) - "LegalBench: A Collaboratively Built Benchmark for Measuring Legal Reasoning in Large Language Models"
5. Zheng et al. (2021) - "When Does Pretraining Help? Assessing Self-Supervised Learning for Law and the CaseHOLD Dataset"

### Multi-tenancy
6. Guo et al. (2007) - "A Framework for Native Multi-Tenancy Application Development and Management"
7. Bezemer & Zaidman (2010) - "Multi-Tenant SaaS Applications: Maintenance Dream or Nightmare?"

### Microservices
8. Richardson, C. (2018) - "Microservices Patterns: With Examples in Java"
9. Newman, S. (2021) - "Building Microservices: Designing Fine-Grained Systems" (2nd Edition)

### Vietnamese NLP
10. Nguyen & Nguyen (2020) - "PhoBERT: Pre-trained language models for Vietnamese"

---

**KẾT THÚC CHƯƠNG 2**

**Điểm nhấn chính:**
- ✅ Giải thích rõ các khái niệm nền tảng (RAG, LLM, vector DB, multi-tenancy)
- ✅ Khảo sát đầy đủ nghiên cứu liên quan
- ✅ Phân tích yêu cầu chi tiết (FR, NFR, use cases)
- ✅ Lựa chọn công nghệ có căn cứ (bảng so sánh)
- ✅ Xác định rõ research gap
