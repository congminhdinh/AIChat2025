# CHƯƠNG 2: KHẢO SÁT VÀ PHÂN TÍCH HỆ THỐNG

**Định hướng chiến lược:** Hệ thống AIChat2025 không chỉ là công cụ tra cứu pháp lý đơn thuần, mà là nền tảng **Làm giàu tri thức pháp lý tổ chức** (Legal Knowledge Enrichment Platform) - kết nối tri thức nội bộ với khung pháp lý quốc gia, cung cấp ngữ cảnh sâu và thông tin hành động.

**Số trang mục tiêu:** 9-11 trang

---

## 2.1. Khảo sát hiện trạng

**Mục tiêu:** Phân tích các giải pháp AI pháp lý hiện có tại Việt Nam và quốc tế, xác định các khoảng trống về khả năng làm giàu tri thức pháp lý cho tổ chức.

**Số trang:** 2.5-3 trang

### 2.1.1. Phân tích các hệ thống AI pháp lý hiện có

**Nhóm 1: AI pháp lý của Nhà nước**

**Hệ thống: ai.phapluat.gov.vn (Trợ lý ảo Bộ Tư pháp)**
- **Mô tả:** Chatbot tư vấn pháp lý do Bộ Tư pháp phát triển, sử dụng LLM để trả lời câu hỏi về văn bản pháp luật Việt Nam
- **Chức năng chính:**
  - Tra cứu văn bản quy phạm pháp luật (VBQPPL)
  - Trả lời câu hỏi pháp lý chung
  - Hướng dẫn thủ tục hành chính
- **Điểm mạnh:**
  - Dữ liệu chính thống từ cơ sở dữ liệu quốc gia
  - Cập nhật văn bản pháp luật kịp thời
  - Miễn phí cho công dân
- **Khoảng trống (Gaps):**
  - **Thiếu ngữ cảnh tổ chức:** Chỉ trả lời dựa trên luật chung, không hiểu quy định nội bộ của doanh nghiệp cụ thể
  - **Không có multi-tenant isolation:** Mọi người dùng cùng truy cập một knowledge base duy nhất
  - **Không có khả năng làm giàu tri thức:** Không kết nối quy định công ty với khung pháp lý, không cung cấp phân tích độ tuân thủ (compliance)
  - **Thiếu tùy biến:** Không thể cấu hình thuật ngữ ngành nghề riêng hoặc system prompt theo từng lĩnh vực

**Nhóm 2: AI pháp lý thương mại**

**Hệ thống: ailuat.luatvietnam.vn (Trợ lý AI Luật Việt Nam)**
- **Mô tả:** Chatbot của LuatVietnam.vn, tích hợp với cơ sở dữ liệu văn bản pháp luật thương mại
- **Chức năng chính:**
  - Tìm kiếm văn bản pháp luật theo từ khóa
  - Tư vấn pháp lý sơ bộ
  - Tích hợp tra cứu án lệ
- **Điểm mạnh:**
  - Giao diện thân thiện, dễ sử dụng
  - Có trích dẫn nguồn pháp luật
  - Hỗ trợ tìm kiếm nâng cao (theo lĩnh vực, cơ quan ban hành)
- **Khoảng trống:**
  - **Không hỗ trợ dữ liệu nội bộ:** Không thể upload quy chế, nội quy công ty
  - **Thiếu phân tích compliance:** Không so sánh quy định nội bộ với luật quốc gia
  - **Không có business dictionary:** Không ánh xạ thuật ngữ chuyên ngành của công ty với thuật ngữ pháp lý
  - **Single-tenant architecture:** Không phân biệt giữa các tổ chức khác nhau

**Nhóm 3: LLM đa năng**

**ChatGPT (OpenAI), Gemini (Google), Claude (Anthropic)**
- **Mô tả:** Các mô hình ngôn ngữ lớn đa năng, có khả năng trả lời câu hỏi pháp lý trong phạm vi kiến thức đã huấn luyện
- **Điểm mạnh:**
  - Hiểu ngữ cảnh tốt, trả lời tự nhiên
  - Hỗ trợ nhiều ngôn ngữ (bao gồm tiếng Việt)
  - Khả năng reasoning mạnh
- **Khoảng trống:**
  - **Knowledge cutoff:** Không có thông tin pháp luật cập nhật sau thời điểm training
  - **Hallucination risk:** Có thể tạo ra thông tin pháp lý sai lệch, không có trích dẫn đáng tin cậy
  - **Không có knowledge base riêng:** Không thể tích hợp tài liệu nội bộ của tổ chức
  - **Vấn đề bảo mật:** Dữ liệu nhạy cảm có thể bị lưu trữ trên server bên thứ ba
  - **Không có enrichment capability:** Chỉ trả lời câu hỏi, không phân tích mối liên hệ giữa quy định nội bộ và khung pháp lý

**Bảng so sánh tổng hợp:**

| Tiêu chí | ai.phapluat | ailuat.luatvietnam | ChatGPT/Gemini/Claude | **AIChat2025** |
|----------|-------------|--------------------|-----------------------|----------------|
| **Knowledge source** | Luật quốc gia | Luật quốc gia + Án lệ | Pre-trained data | Luật quốc gia + Quy định tổ chức |
| **Multi-tenancy** | ❌ | ❌ | ❌ | ✅ Row-level isolation |
| **Organizational context** | ❌ | ❌ | ❌ | ✅ Upload internal docs |
| **Business dictionary** | ❌ | ❌ | ❌ | ✅ Custom term mapping |
| **Legal Knowledge Enrichment** | ❌ | ❌ | ❌ | ✅ Deep contextualization |
| **Compliance analysis** | ❌ Không | ❌ Không | ❌ Không | ✅ Dual-RAG + Conflict detection |
| **Trích dẫn nguồn** | ⚠️ Cơ bản | ✅ Chi tiết | ❌ Không đáng tin | ✅ Metadata-driven |
| **Data privacy** | ⚠️ Nhà nước | ⚠️ Thương mại | ❌ Third-party | ✅ Self-hosted |
| **Customization** | ❌ Không | ❌ Không | ⚠️ System prompt only | ✅ Prompt + Dictionary + RAG config |

### 2.1.2. Xác định khoảng trống nghiên cứu

**Gap 1: Thiếu khả năng làm giàu tri thức pháp lý cho tổ chức (Legal Knowledge Enrichment)**
- **Vấn đề:** Các hệ thống hiện tại chỉ cung cấp thông tin pháp lý tĩnh, không tạo ra tri thức mới bằng cách kết nối quy định nội bộ với khung pháp lý quốc gia
- **Nhu cầu thực tế:** 
  - Doanh nghiệp cần hiểu sâu sắc **tại sao** một quy định nội bộ được thiết kế theo cách nào, **cơ sở pháp lý** nào hỗ trợ nó
  - HR cần biết quy định của công ty có **tuân thủ** pháp luật hay không, và **cách điều chỉnh** nếu có xung đột
- **Giải pháp AIChat2025:** 
  - **Hierarchical contextualization:** Khi trả lời về một điều khoản, hệ thống tự động cung cấp ngữ cảnh từ chương/điều cao hơn và liên kết với điều luật quốc gia tương ứng
  - **Knowledge graph enrichment:** Xây dựng mối quan hệ giữa quy định công ty và luật quốc gia (ví dụ: "Điều 15 Nội quy → Tuân thủ Điều 112 BLLĐ 2019")

**Gap 2: Thiếu khả năng tùy biến theo ngành nghề (Business Dictionary & Domain-specific Prompts)**
- **Vấn đề:** Thuật ngữ chuyên ngành (fintech, manufacturing, healthcare) không được các hệ thống hiện tại hiểu đúng
- **Ví dụ:** 
  - Trong fintech: "T+0" có nghĩa là "thanh toán ngay trong ngày" (không phải "Temperature 0")
  - Trong logistics: "COD" là "Cash on Delivery" (không phải "Call of Duty")
- **Giải pháp AIChat2025:**
  - **Business Dictionary:** Tenant Admin cấu hình ánh xạ thuật ngữ công ty → thuật ngữ pháp lý chuẩn
  - **Custom System Prompts:** Mỗi tenant có thể định nghĩa role của AI phù hợp với ngành nghề (ví dụ: "Bạn là chuyên gia pháp lý ngân hàng")

**Gap 3: Thiếu cơ chế cô lập dữ liệu đa tổ chức (Multi-tenant Data Isolation)**
- **Vấn đề:** Các hệ thống công cộng (ai.phapluat, ailuat) không phân biệt giữa các công ty khác nhau
- **Rủi ro bảo mật:** Nếu dùng chung một knowledge base, có nguy cơ rò rỉ dữ liệu nội bộ giữa các tổ chức
- **Giải pháp AIChat2025:**
  - Row-level tenant isolation với EF Core Global Query Filter
  - Qdrant collection separation (`tenant_1`, `tenant_2`, ...)
  - MinIO bucket isolation (`tenant-1`, `tenant-2`)

**Gap 4: Thiếu kiến trúc phân tầng tri thức (Hierarchical Knowledge Architecture)**
- **Vấn đề:** Các hệ thống RAG thông thường chunk theo fixed-size, phá vỡ cấu trúc logic của văn bản pháp luật
- **Đặc thù pháp luật Việt Nam:** Có cấu trúc rõ ràng (Chương → Điều → Khoản → Điểm)
- **Giải pháp AIChat2025:**
  - Hierarchical semantic chunking giữ nguyên cấu trúc phân cấp
  - Metadata-driven retrieval: Tìm kiếm theo hierarchy level (tìm cả Chương khi cần big picture, hoặc chỉ Khoản khi cần chi tiết)

**Kết luận Gap Analysis:**
> AIChat2025 lấp đầy khoảng trống bằng cách kết hợp: **Legal Knowledge Enrichment** (làm giàu tri thức) + **Business Dictionary** (tùy biến ngành nghề) + **Multi-tenant Isolation** (bảo mật dữ liệu) + **Hierarchical Chunking** (bảo toàn cấu trúc pháp luật)

---

## 2.2. Tổng quan chức năng hệ thống

**Mục tiêu:** Xác định các tác nhân, nhóm chức năng chính, và quy trình nghiệp vụ cốt lõi của hệ thống.

**Số trang:** 2-2.5 trang

### 2.2.1. Xác định tác nhân (Actors)

**Actor 1: System Admin (Quản trị viên hệ thống)**
- **Vai trò:** Quản lý toàn bộ hệ thống, tạo và giám sát các tenant
- **Trách nhiệm:**
  - Tenant provisioning (tạo tenant mới)
  - Quản lý legal knowledge base chung (tenant_id=1: Luật quốc gia)
  - System monitoring và troubleshooting

**Actor 2: Tenant Admin (Quản trị viên tổ chức - Power User)**
- **Vai trò:** Quản lý tri thức nội bộ và cấu hình AI cho tổ chức, đồng thời là người dùng chính
- **Trách nhiệm:**
  - **Knowledge Management:** Upload/quản lý tài liệu nội bộ (Nội quy, Quy chế, Hợp đồng mẫu)
  - **Business Dictionary Configuration:** Cấu hình ánh xạ thuật ngữ chuyên ngành
    - Ví dụ: "Thời gian settlement" → "Thời gian thanh toán" (fintech)
    - Ví dụ: "Thời gian lead time" → "Thời gian chu kỳ sản xuất" (manufacturing)
  - **System Prompt Customization:** Định nghĩa role của AI phù hợp với ngành nghề
    - Ví dụ: "Bạn là chuyên gia pháp lý ngân hàng, chuyên về quy định thanh toán và chống rửa tiền"
  - **User Management:** Quản lý End User accounts trong công ty
  - **Perform Chat:** Sử dụng AI để tư vấn (giống End User nhưng có nhiều quyền hơn)

**Actor 3: End User (Nhân viên/người dùng cuối)**
- **Vai trò:** Sử dụng chatbot để hỏi đáp về quy định pháp lý
- **Trách nhiệm:**
  - Hỏi về quy định công ty hoặc luật quốc gia
  - Xem lịch sử chat
  - Đánh giá chất lượng câu trả lời

**Sơ đồ:**
- Diagram 2.1: Actor Hierarchy và phân quyền

### 2.2.2. Phân nhóm chức năng (Functional Modules)

**Module 1: Identity & Multi-tenant Access Control**
- User authentication (JWT-based)
- Role-based authorization (SystemAdmin, TenantAdmin, EndUser)
- Tenant provisioning và isolation

**Module 2: Knowledge Enrichment Processing**
- Document upload và parsing (.docx, .pdf)
- Hierarchical semantic chunking
- Business Dictionary application (term mapping before embedding)
- Dual knowledge base management (Organizational KB + Legal KB)

**Module 3: Enriched RAG Chat**
- Intent detection (Query classification)
- Multi-source hybrid retrieval (Organizational + Legal)
- Contextual enrichment (thêm hierarchy context + related legal articles)
- LLM generation với custom system prompt
- Real-time streaming response (SignalR)

**Module 4: Configuration & Analytics**
- Business Dictionary editor
- System Prompt configurator
- Chat history analytics (usage reports)

**Sơ đồ:**
- Diagram 2.2: Functional Modules Architecture

### 2.2.3. Quy trình nghiệp vụ cốt lõi

**Business Process 1: Legal Knowledge Enrichment Pipeline**

Quy trình từ khi upload tài liệu đến khi tri thức được vectorize và sẵn sàng để retrieval.

**Luồng chính:**
```
[Start] Tenant Admin upload document (Nội quy lao động.docx)
    ↓
[MinIO Upload] Lưu file vào MinIO object storage (path: tenant-{id}/documents/...)
    ↓
[Database Record] Tạo PromptDocument record (Status = "Upload", TenantId auto-injected)
    ↓
[Hangfire Job] Trigger VectorizeBackgroundJob.ProcessBatch()
    ↓
[Document Parsing]
    - Download từ MinIO
    - Parse với DocumentFormat.OpenXml (.docx) hoặc PdfPig (.pdf)
    - Phát hiện cấu trúc bằng Regex: Chương (Heading1), Mục (Heading2), Điều (Content)
    ↓
[Hierarchical Chunking]
    - Tạo chunks ở mức Article-level (Điều)
    - Mỗi chunk bao gồm: Heading1 (Chương) + Heading2 (Mục) + Content (Điều + body)
    - FullText = Heading1 + Heading2 + Content (đây là text được embed)
    - Metadata: {source_id, file_name, document_name, father_doc_name, heading1, heading2, content, tenant_id, type}
    ↓
[Batch Processing] Chia thành batches (10 chunks/batch)
    ↓
[Embedding (Python)]
    - Gọi EmbeddingService.VectorizeBatch() API
    - Model: truro7/vn-law-embedding (768-dim vectors)
    ↓
[Qdrant Upsert]
    - Lưu vào collection `vn_law_documents` (single collection cho tất cả tenants)
    - Metadata lưu kèm: source_id, heading1, heading2, content, tenant_id, type
    - Tenant isolation qua metadata field `tenant_id`
    ↓
[Status Update] PromptDocument.Action = "Vectorize_Success"
    ↓
[End] Tri thức sẵn sàng để RAG retrieval
```

**Lưu ý quan trọng:**
- KHÔNG có Business Dictionary application trong chunking phase
- KHÔNG có automatic legal linking trong chunking phase
- KHÔNG có compliance pre-check
- Father-child relationship (NghiDinh → Luat) là manual, không tự động phát hiện

**Business Process 2: Enriched RAG Query Flow**

Quy trình từ khi End User hỏi câu hỏi đến khi nhận được câu trả lời qua SignalR streaming.

**Luồng chính:**
```
[Start] User nhập: "Quy định OT trong công ty như thế nào?"
    ↓
[SignalR Input] ChatHub.SendMessage() nhận message từ frontend
    ↓
[ChatBusiness (C#)]
    - Lưu user message vào ChatMessage table (TenantId auto-injected)
    - Lookup PromptConfig (Business Dictionary) bằng LIKE query
      → Tìm thấy: {Key: "OT", Value: "Làm thêm giờ (Overtime)"}
    - Publish UserPromptReceivedEvent lên RabbitMQ với SystemInstruction
    ↓
[RabbitMQ] Event gửi đến ChatProcessor (Python)
    ↓
[ChatProcessor Consumer]
    - Decode JWT token → Extract tenant_id, user_id
    - Consume UserPromptReceivedEvent
    ↓
[Query Expansion]
    - Hàm: _expand_query_with_prompt_config()
    - Replace "OT" → "Làm thêm giờ (Overtime)"
    - Enhanced query: "Quy định Làm thêm giờ (Overtime) trong công ty như thế nào?"
    ↓
[Legal Keyword Extraction]
    - LegalTermExtractor phát hiện: "OT", "làm thêm giờ", "overtime"
    - Thêm patterns: "Điều X", "Bộ luật X", v.v.
    ↓
[Embedding] Gọi EmbeddingService.embed() → 768-dim vector
    ↓
[Hybrid Search (RRF)]
    - Vector Search: Semantic similarity search (top-10)
    - Keyword Search: BM25 với legal keywords (top-10)
    - RRF Fusion: score(doc) = Σ 1/(60 + rank_i)
    - Filter: tenant_id IN (1, current_tenant_id)
    - Collection: vn_law_documents
    ↓
[Result Separation]
    - tenant_results: docs có tenant_id = current_tenant_id (quy định công ty)
    - global_results: docs có tenant_id = 1 (luật quốc gia)
    ↓
[Fallback Logic]
    - Nếu quality_tenant_results < 2: FALLBACK triggered → Ưu tiên global docs
    - Ngược lại: Balanced split (60% tenant, 40% global)
    ↓
[Scenario Detection]
    - "BOTH": Có cả tenant + global docs
    - "COMPANY_ONLY": Chỉ tenant docs
    - "LEGAL_ONLY": Chỉ global docs
    - "NONE": Không tìm thấy → Return error
    ↓
[Context Structure]
    - Hàm: _structure_context_for_compliance()
    - Format:
      I. QUY ĐỊNH CÔNG TY
         [Nguồn: <document_name>]
         Chương: <heading1>
         Mục: <heading2>
         Nội dung: <content>
      II. LUẬT QUỐC GIA
         [Nguồn: Bộ luật Lao động]
         Chương: <heading1>
         Nội dung: <content>
    ↓
[System Prompt Selection]
    - Scenario = "BOTH": "Trả lời dựa trên QUY ĐỊNH CÔNG TY, bổ sung LUẬT QUỐC GIA"
    - Scenario = "LEGAL_ONLY": "Không tìm thấy quy định công ty, dựa trên LUẬT"
    ↓
[Terminology Injection]
    - _build_terminology_definitions()
    - Append vào conversation: "THUẬT NGỮ: - OT: Làm thêm giờ (Overtime)"
    ↓
[LLM Generation]
    - Model: ontocord/vistral:latest (Ollama local)
    - Temperature: 0.1 (factual)
    - Stream response
    ↓
[Response Publishing]
    - Publish BotResponseCreatedEvent lên RabbitMQ
    ↓
[BotResponseConsumer (C#)]
    - Consume event
    - Decode token → Set CurrentTenantProvider.TenantId
    - Lưu bot message vào ChatMessage table
    - Broadcast qua SignalR: ChatHub.BroadcastBotResponse()
    ↓
[Frontend Render] User nhận câu trả lời qua SignalR event "BotResponse"
    ↓
[End]
```

**Lưu ý quan trọng:**
- KHÔNG có separate Qdrant collections per tenant (dùng single collection với tenant_id filter)
- KHÔNG có hierarchical context injection (chunks đã chứa full hierarchy trong FullText)
- KHÔNG có automatic compliance detection/conflict highlighting
- Hierarchical context (Chương → Mục → Điều) đã được lưu trong metadata khi chunking

**Sơ đồ:**
- Diagram 2.3: Activity Diagram - Legal Knowledge Enrichment Pipeline
- Diagram 2.4: Activity Diagram - Enriched RAG Query Flow

---

## 2.3. Đặc tả chức năng chi tiết

**Mục tiêu:** Mô tả chi tiết 4 use case quan trọng nhất, tập trung vào các tính năng độc đáo của AIChat2025.

**Số trang:** 3-3.5 trang

### 2.3.1. UC1: Hierarchical Knowledge Enrichment Processing

**Tên:** Xử lý làm giàu tri thức phân cấp

**Actor:** System (Background Job - Hangfire)

**Mục tiêu:** Tự động parse tài liệu, chunk theo cấu trúc phân cấp pháp lý Việt Nam, và vectorize để chuẩn bị cho retrieval.

**Precondition:**
- Tài liệu đã được upload qua `StorageService` và lưu trong MinIO
- `PromptDocument.Action = "Upload"` (Status ban đầu)
- `TenantId` đã được gán cho document

**Main Flow:**

1. **Trigger Background Job:**
   - `PromptDocumentBusiness.VectorizeDocument()` được gọi sau khi upload
   - Hangfire enqueue `VectorizeBackgroundJob.ProcessBatch(batch, tenantId)`
   - Class: `Services\DocumentService\Features\VectorizeBackgroundJob.cs`

2. **Phase 1: Document Download & Parsing**
   - Download file từ MinIO qua `StorageBusiness.GetObject()`
   - Đọc với DocumentFormat.OpenXml (.docx) hoặc UglyToad.PdfPig (.pdf)
   - Class: `PromptDocumentBusiness.ExtractHierarchicalChunks()`
   - Location: `Services\DocumentService\Features\PromptDocumentBusiness.cs:355-420`

3. **Phase 2: Hierarchical Structure Detection**
   - Áp dụng Regex patterns từ appsettings.json:
     ```json
     {
       "RegexHeading1": "^\\s*Chương\\s+[IVXLCDM]+\\b",  // Chương I, II, III...
       "RegexHeading2": "^\\s*Mục\\s+\\d+\\b",            // Mục 1, 2, 3...
       "RegexHeading3": "^\\s*Điều\\s+\\d+\\b"            // Điều 1, 2, 3...
     }
     ```
   - Phát hiện cấu trúc 3 cấp:
     ```
     Heading1: Chương III (Chapter)
       Heading2: Mục 1 (Section) - optional
         Content: Điều 15 + body paragraphs (Article + content)
     ```

4. **Phase 3: Hierarchical Chunking**
   - Tạo chunks ở mức **Article-level (Điều)**:
     ```csharp
     DocumentChunkDto {
       DocumentName: "Bộ luật Lao động 2019",
       DocumentType: DocType.Luat,  // 0=Initial, 1=Luat, 2=NghiDinh
       FatherDocumentName: null,     // Chỉ có giá trị nếu DocumentType = NghiDinh
       Heading1: "Chương III",       // Chapter context
       Heading2: "Mục 1",            // Section context (có thể null)
       Content: "Điều 15\n<body paragraphs>",  // Article + content
       FullText: "Chương III\nMục 1\nĐiều 15\n<body>",  // Text được embed
       DocumentId: 123,
       FileName: "bo_luat_lao_dong.docx"
     }
     ```
   - **FullText composition** (line 444-450):
     - Kết hợp: `Heading1 + Heading2 + Content`
     - Đây là text được gửi đến EmbeddingService để tạo vector

5. **Phase 4: Batch Vectorization**
   - Chia chunks thành batches (10 chunks/batch)
   - Gọi `EmbeddingService.VectorizeBatch()` API (Python FastAPI)
   - Request payload:
     ```json
     {
       "documents": [
         {
           "text": "<FullText của chunk>",
           "metadata": {
             "source_id": 123,
             "file_name": "bo_luat_lao_dong.docx",
             "document_name": "Bộ luật Lao động 2019",
             "father_doc_name": null,
             "heading1": "Chương III",
             "heading2": "Mục 1",
             "content": "Điều 15...",
             "tenant_id": 5,
             "type": 1
           }
         }
       ]
     }
     ```

6. **Phase 5: Embedding Generation (Python)**
   - Model: `truro7/vn-law-embedding` (Vietnamese legal text embedding)
   - Vector dimensions: 768
   - Class: `EmbeddingService.vectorize_batch()`
   - Location: `Services\EmbeddingService\src\business.py`

7. **Phase 6: Qdrant Upsert**
   - **Collection:** `vn_law_documents` (single collection cho tất cả tenants)
   - **Tenant isolation:** Qua metadata field `tenant_id`
   - Metadata schema lưu trong Qdrant:
     ```python
     {
       "source_id": 123,               # Document ID từ SQL
       "file_name": "...",
       "document_name": "...",
       "father_doc_name": "...",       # Null hoặc tên Luật cha (cho NghiDinh)
       "heading1": "Chương III",       # Ngữ cảnh Chapter
       "heading2": "Mục 1",            # Ngữ cảnh Section
       "content": "Điều 15...",        # Nội dung Article
       "tenant_id": 5,                 # Tenant isolation key
       "type": 1                       # 0=Initial, 1=Luat, 2=NghiDinh
     }
     ```

8. **Phase 7: Status Update**
   - Nếu thành công:
     - `PromptDocument.Action = "Vectorize_Success"`
   - Nếu thất bại:
     - `PromptDocument.Action = "Vectorize_Failed"`
   - Class: `VectorizeBackgroundJob.Execute()`

**Alternate Flow:**
- **A1:** Document không có cấu trúc rõ ràng (thiếu Heading styles)
  - Fallback: Chunk theo fixed-size (1000 tokens)
  - Heading1/Heading2 = empty string
- **A2:** API EmbeddingService timeout
  - Hangfire retry 3 lần với exponential backoff
  - Sau 3 lần: Mark document as "Vectorize_Failed"

**Postcondition:**
- Chunks được lưu trong Qdrant collection `vn_law_documents` với `tenant_id` filtering
- Document status = "Vectorize_Success"
- Tri thức sẵn sàng cho RAG retrieval với đầy đủ ngữ cảnh phân cấp (Chương → Mục → Điều)

**Key Implementation Details:**
- **Không có separate collections per tenant** - Dùng single collection với `tenant_id` filter
- **Không có Business Dictionary application trong chunking phase** - Business Dictionary chỉ áp dụng trong RAG query phase
- **Không có automatic legal linking** - Father-child relationship chỉ có cho NghiDinh → Luat (manual)

---

### 2.3.2. UC2: Enriched RAG Chat (Multi-source Contextual Retrieval)

**Tên:** Hỏi đáp RAG với làm giàu ngữ cảnh đa nguồn và hybrid search

**Actor:** End User hoặc Tenant Admin

**Mục tiêu:** Trả lời câu hỏi với ngữ cảnh được làm giàu từ cả quy định nội bộ (organizational) và luật quốc gia (legal), sử dụng hybrid search (semantic + keyword) với RRF re-ranking.

**Precondition:**
- User đã đăng nhập
- Có ít nhất 1 document đã vectorized (status = "Vectorize_Success")
- `PromptConfig` (Business Dictionary) đã được cấu hình (optional)

**Main Flow:**

1. **User Input via SignalR:**
   - User nhập query trong chat: "Quy định OT trong công ty như thế nào?"
   - Frontend gửi qua SignalR Hub: `ChatHub.SendMessage(conversationId, message, userId)`
   - Class: `Services\ChatService\Hubs\ChatHub.cs`

2. **Message Persistence & Event Publishing (C#):**
   - `ChatBusiness.SaveUserMessageAndPublishAsync()` xử lý:
     - Lưu user message vào `ChatMessage` table (TenantId auto-injected)
     - Lookup `PromptConfig` bằng `PromptConfigByMessageSpec`:
       ```csharp
       // Tìm tất cả configs có Key xuất hiện trong message
       Query.Where(x => EF.Functions.Like(message, "%" + x.Key + "%"));
       ```
     - Publish `UserPromptReceivedEvent` lên RabbitMQ:
       ```csharp
       {
         ConversationId: 123,
         Message: "Quy định OT trong công ty như thế nào?",
         Token: "<JWT token>",  // Chứa TenantId và UserId
         Timestamp: DateTime.UtcNow,
         SystemInstruction: [
           { Key: "OT", Value: "Làm thêm giờ (Overtime)" }
         ]
       }
       ```
   - Class: `Services\ChatService\Features\ChatBusiness.cs`
   - Queue: `UserPromptReceived` (fanout exchange)

3. **Message Consumption (Python ChatProcessor):**
   - `RabbitMQService` consume event từ queue
   - Decode JWT token để lấy `tenant_id` và `user_id`
   - Gọi `ChatBusiness.process_chat_message()`
   - Class: `Services\ChatProcessor\src\consumer.py`

4. **Query Expansion với Business Dictionary:**
   - Hàm: `_expand_query_with_prompt_config()`
   - Location: `Services\ChatProcessor\src\business.py:639-682`
   - Logic:
     ```python
     enhanced_message = "Quy định OT trong công ty như thế nào?"
     for config_item in system_instruction:
       if config_item['key'] in enhanced_message:
         enhanced_message = enhanced_message.replace(
           config_item['key'],
           config_item['value']
         )
     # Result: "Quy định Làm thêm giờ (Overtime) trong công ty như thế nào?"
     ```

5. **Legal Keyword Extraction:**
   - Class: `LegalTermExtractor` (`Services\ChatProcessor\src\hybrid_search.py`)
   - Extract:
     - Article patterns: "Điều 212 khoản 1"
     - Law codes: "Bộ luật Lao động"
     - Decrees: "Nghị định 145/2020/NĐ-CP"
     - Abbreviations từ Business Dictionary: "OT", "Làm thêm giờ"
   - Output: `keywords = ['ot', 'làm thêm giờ', 'overtime', 'điều', '212']`

6. **Embedding Generation:**
   - Gọi `EmbeddingService.embed()` với enhanced query
   - Model: `truro7/vn-law-embedding`
   - Output: 768-dim vector

7. **Dual-source Hybrid Search với RRF:**
   - Hàm: `QdrantService.hybrid_search_with_fallback()`
   - Location: `Services\ChatProcessor\src\hybrid_search.py:196-301`

   **Step 7a: Vector Search (Semantic)**
   ```python
   qdrant_client.search(
     collection_name="vn_law_documents",
     query_vector=query_embedding,
     query_filter=Filter(should=[
       FieldCondition(key='tenant_id', match=MatchValue(value=1)),      # Global legal
       FieldCondition(key='tenant_id', match=MatchValue(value=tenant_id))  # Tenant-specific
     ]),
     limit=10
   )
   ```

   **Step 7b: Keyword Search (BM25)**
   ```python
   qdrant_client.search(
     collection_name="vn_law_documents",
     query_filter=<same as vector search>,
     search_params=SearchParams(
       quantization=QuantizationSearchParams(
         ignore=False,
         rescore=True
       )
     ),
     limit=10,
     # Uses BM25 on 'content' field với legal_keywords
   )
   ```

   **Step 7c: Reciprocal Rank Fusion (RRF)**
   - Class: `ReciprocalRankFusion.fuse()`
   - Formula: `score(doc) = Σ 1/(60 + rank_i(doc))`
   - Combines top-10 từ vector search + top-10 từ keyword search
   - Output: Ranked list theo RRF score

8. **Result Separation & Fallback Logic:**
   - Tách kết quả theo `tenant_id`:
     - `tenant_results` = docs có `tenant_id == current_tenant_id` (quy định công ty)
     - `global_results` = docs có `tenant_id == 1` (luật quốc gia)

   - **Fallback condition** (line 232-266):
     ```python
     quality_tenant_results = [r for r in tenant_results if r.score >= 0.7]

     if len(quality_tenant_results) < 2:
       # FALLBACK: Ưu tiên luật quốc gia
       fallback_triggered = True
       tenant_limit = min(len(tenant_results), 2)
       global_limit = 5 - tenant_limit
     else:
       # NORMAL: Balanced split (60% tenant, 40% global)
       fallback_triggered = False
       tenant_limit = max(1, int(5 * 0.6))  # 3 chunks
       global_limit = 5 - tenant_limit       # 2 chunks
     ```

9. **Scenario Detection:**
   - Hàm: `_detect_scenario()` (line 612-637)
   - Scenarios:
     - `"BOTH"`: Có cả tenant + global results
     - `"COMPANY_ONLY"`: Chỉ có tenant results
     - `"LEGAL_ONLY"`: Chỉ có global results
     - `"NONE"`: Không tìm thấy gì

10. **Context Structure for LLM:**
    - Hàm: `_structure_context_for_compliance()` (line 714-774)
    - Format:
      ```python
      NGUYÊN TẮC TRẢ LỜI:
      - Ưu tiên quy định công ty nếu có
      - Bổ sung luật quốc gia làm cơ sở pháp lý

      I. QUY ĐỊNH CÔNG TY:
      [Nguồn 1: <document_name>]
      Chương: <heading1>
      Mục: <heading2>
      Nội dung: <content>

      II. LUẬT QUỐC GIA (CƠ SỞ PHÁP LÝ):
      [Nguồn 2: Bộ luật Lao động 2019]
      Chương: Chương XII
      Mục: Mục 2
      Nội dung: Điều 107. Làm thêm giờ...
      ```

11. **System Prompt Selection:**
    - Scenario = "BOTH":
      ```python
      "Bạn là chuyên gia pháp lý. Trả lời dựa trên QUY ĐỊNH CÔNG TY trước,
       sau đó bổ sung LUẬT QUỐC GIA làm cơ sở pháp lý."
      ```
    - Scenario = "LEGAL_ONLY":
      ```python
      "Không tìm thấy quy định công ty. Trả lời dựa trên LUẬT QUỐC GIA."
      ```

12. **Terminology Injection:**
    - Hàm: `_build_terminology_definitions()` (line 684-710)
    - Append vào conversation history:
      ```python
      {
        'role': 'system',
        'content': '''
        THUẬT NGỮ CHUYÊN MÔN:
        - OT: Làm thêm giờ (Overtime)
        '''
      }
      ```

13. **LLM Generation (Ollama):**
    - Model: `ontocord/vistral:latest`
    - Temperature: 0.1 (factual answers)
    - Class: `OllamaService.generate_response()`
    - Location: `Services\ChatProcessor\src\business.py:140-203`

14. **Response Post-processing:**
    - Hàm: `_cleanup_response()` (line 776-850)
    - Remove markdown artifacts, normalize whitespace

15. **Response Publishing (RabbitMQ):**
    - Publish `BotResponseCreatedEvent`:
      ```python
      {
        ConversationId: 123,
        Message: "<AI generated answer>",
        Token: "<original JWT>",
        Timestamp: DateTime.UtcNow,
        ModelUsed: "vistral"
      }
      ```
    - Queue: `BotResponseCreated`

16. **Response Consumption & SignalR Broadcast (C#):**
    - `BotResponseConsumer` consume event
    - Decode token → Set `CurrentTenantProvider.TenantId`
    - Lưu bot message vào `ChatMessage` table
    - Broadcast qua SignalR:
      ```csharp
      await ChatHub.BroadcastBotResponse(hubContext, conversationId, messageDto);
      ```
    - Class: `Services\ChatService\Consumers\BotResponseConsumer.cs`

17. **Frontend Rendering:**
    - SignalR client nhận event "BotResponse"
    - Render với markdown formatting

**Alternate Flow:**
- **A1:** Scenario = "NONE" (không tìm thấy vectors)
  - Response: "Xin lỗi, tôi không tìm thấy thông tin liên quan..."
  - Không gọi LLM

- **A2:** `fallback_triggered = True`
  - System prompt thêm:
    ```
    LƯU Ý: Không tìm thấy đủ quy định công ty chất lượng cao.
    Ưu tiên trả lời dựa trên luật quốc gia.
    ```

- **A3:** EmbeddingService hoặc Ollama timeout
  - Return error response
  - Frontend hiển thị: "Hệ thống đang quá tải, vui lòng thử lại"

**Postcondition:**
- User nhận câu trả lời với:
  - Ngữ cảnh phân cấp đầy đủ (Chương → Mục → Điều)
  - Dual-source context (quy định công ty + luật quốc gia)
  - Business terms được expand và định nghĩa
- Chat history được lưu với TenantId isolation

**Key Implementation Details:**
- **Hybrid Search:** RRF combines semantic (vector) + keyword (BM25) search
- **Fallback Strategy:** Tự động switch sang luật quốc gia nếu thiếu quy định công ty chất lượng
- **Single Collection Strategy:** Dùng `vn_law_documents` cho tất cả tenants, filter bằng `tenant_id`
- **Business Dictionary:** Chỉ áp dụng trong query phase (query expansion + terminology injection), KHÔNG lưu trong metadata
- **No Compliance Detection:** Không có automatic conflict detection giữa quy định công ty và luật quốc gia

---

### 2.3.3. UC3: Tenant-level Access Control & Data Isolation

**Tên:** Kiểm soát truy cập và cô lập dữ liệu theo tenant

**Actor:** System (Automated Enforcement - Middleware & Interceptors)

**Mục tiêu:** Đảm bảo mọi truy vấn (database, vector DB, object storage) chỉ trả về dữ liệu của tenant hiện tại, tuyệt đối không rò rỉ sang tenant khác.

**Precondition:**
- User đã đăng nhập và nhận JWT token có claim `TenantId`

**Main Flow:**

1. **User Authentication & Token Generation:**
   - User login qua `AuthBusiness.LoginAsync(email, password)`
   - Verify credentials với BCrypt password hashing
   - Tạo JWT token với claims:
     ```csharp
     new Claim(AuthorizationConstants.TOKEN_CLAIMS_TENANT, account.TenantId.ToString()),
     new Claim(AuthorizationConstants.TOKEN_CLAIMS_USER, account.Id.ToString()),
     new Claim(ClaimTypes.Email, account.Email),
     new Claim(ClaimTypes.Role, account.IsAdmin ? "Admin" : "User")
     ```
   - Class: `Services\AccountService\Features\AuthBusiness.cs`

2. **HTTP Request Authorization (Web Requests):**
   - Mọi API request attach JWT token trong header:
     ```
     Authorization: Bearer <JWT_TOKEN>
     ```
   - `CurrentUserProvider` extract claims từ `HttpContext.User`:
     ```csharp
     public int TenantId
     {
       get
       {
         var tenantClaim = _httpContextAccessor?.HttpContext?.User?
           .FindFirstValue(AuthorizationConstants.TOKEN_CLAIMS_TENANT);
         return int.Parse(tenantClaim ?? "0");
       }
     }
     ```
   - Class: `Infrastructure\Web\CurrentUserProvider.cs`

3. **Background Process Tenant Context (RabbitMQ Consumers):**
   - Khi consume message từ RabbitMQ, không có `HttpContext`
   - Sử dụng `ICurrentTenantProvider` với manual injection:
     ```csharp
     // In BotResponseConsumer.cs
     var tokenInfo = TokenDecoder.DecodeJwtToken(botResponse.Token);
     var tenantId = TokenDecoder.GetTenantId(tokenInfo);
     _currentTenantProvider.SetTenantId(tenantId);  // Manual set
     ```
   - `CurrentTenantProvider` hybrid provider:
     ```csharp
     public int TenantId
     {
       get
       {
         // Try HTTP context first
         if (_currentUserProvider != null)
         {
           var fromContext = _currentUserProvider.TenantId;
           if (fromContext > 0) return fromContext;
         }
         // Fallback to manual injection
         return _manualTenantId ?? 0;
       }
     }
     ```
   - Class: `Infrastructure\Tenancy\CurrentTenantProvider.cs`

4. **Database Query Filtering (EF Core - Automatic):**

   **4a. Base Entity with TenantId:**
   ```csharp
   // Infrastructure\Entities\BaseEntity.cs
   public abstract class TenancyEntity : AuditableEntity
   {
     public int TenantId { get; set; }  // Auto-injected
   }
   ```

   **4b. UpdateTenancyInterceptor (Auto-inject TenantId on Save):**
   ```csharp
   // Infrastructure\Database\UpdateTenancyInterceptor.cs
   public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
   {
     var entries = context.ChangeTracker.Entries<TenancyEntity>();
     var currentTenantId = _tenantProvider.TenantId;

     if (currentTenantId != 1 && currentTenantId > 0)
     {
       foreach (var entry in entries.Where(e => e.State == EntityState.Added))
       {
         entry.Entity.TenantId = currentTenantId;  // Auto-inject
       }
     }
   }
   ```
   - **Special rule:** `TenantId = 1` là GLOBAL (luật quốc gia, accessible cho tất cả tenants)

   **4c. TenancySpecification (Base specification cho filtering):**
   ```csharp
   // Infrastructure\Specifications\TenancySpecification.cs
   public abstract class TenancySpecification<T> : Specification<T>
     where T : TenancyEntity
   {
     protected TenancySpecification(int tenantId) : base()
     {
       if (tenantId != 1 && tenantId > 0)
       {
         Query.Where(e => e.TenantId == tenantId || e.TenantId == 1);
       }
     }
   }
   ```
   - **Logic:** Mọi query đều filter `WHERE (TenantId = currentTenant OR TenantId = 1)`
   - TenantId = 1 (global docs) luôn accessible

   **4d. Example Query:**
   ```csharp
   // Developer viết:
   var messages = await _chatMessageRepo.ListAsync(spec);

   // Specification tự động thêm filter:
   // SELECT * FROM ChatMessages
   // WHERE (TenantId = 5 OR TenantId = 1) AND !IsDeleted
   ```

5. **Qdrant Vector DB Filtering (Metadata-based):**
   - **KHÔNG có separate collections per tenant**
   - Dùng **single collection** `vn_law_documents` với metadata filtering:
     ```python
     # Services\ChatProcessor\src\business.py:112-117
     search_filter = Filter(
       should=[
         FieldCondition(key='tenant_id', match=MatchValue(value=1)),          # Global
         FieldCondition(key='tenant_id', match=MatchValue(value=tenant_id))  # Tenant
       ]
     )

     results = qdrant_client.search(
       collection_name="vn_law_documents",
       query_vector=embedding,
       query_filter=search_filter,
       limit=10
     )
     ```
   - **Tenant isolation:** Filter theo metadata `tenant_id`
   - **Global access:** TenantId = 1 luôn được include

6. **MinIO Object Storage (Path-based Isolation):**
   - **KHÔNG có separate buckets per tenant**
   - Dùng **single bucket** với path prefix:
     ```csharp
     // Services\StorageService\Features\StorageBusiness.cs
     public async Task<BaseResponse<StringValueDto>> UploadObject(UploadMinioRequest file)
     {
       var minioClient = NewMinIOClient();
       // File path includes tenant context via Directory parameter
       var filePath = Path.Combine(file.Directory, newFileName);
       // Example: "tenant-5/documents/file.docx"

       await minioClient.PutObjectAsync(
         new PutObjectArgs()
           .WithBucket(_appSettings.MinioBucket)  // Same bucket
           .WithObject(filePath)                   // Tenant-specific path
           .WithStreamData(stream)
       );
     }
     ```
   - **Tenant isolation:** Qua directory prefix trong path
   - Caller phải truyền `Directory` parameter phù hợp với TenantId

7. **Cross-tenant Access Prevention:**
   - Tất cả entities kế thừa `TenancyEntity` đều có auto-filtering
   - Nếu user cố query resource của tenant khác:
     ```
     GET /api/documents/123
     ```
   - EF Core Global Filter hoặc TenancySpecification tự động chặn:
     ```csharp
     var doc = await _promptDocumentRepo.GetByIdAsync(123);
     // Returns null nếu doc.TenantId != currentTenantId (và != 1)
     ```
   - Không cần manual authorization check trong controller

8. **Tenant Context Validation (Defensive Check):**
   - Một số critical operations có thêm validation:
     ```csharp
     public async Task VectorizeDocument(int documentId)
     {
       var doc = await _repo.GetByIdAsync(documentId);
       if (doc.TenantId != _currentUserProvider.TenantId && doc.TenantId != 1)
       {
         throw new UnauthorizedAccessException("Cross-tenant access denied");
       }
     }
     ```

**Security Layers:**

| Layer | Mechanism | Implementation | Status |
|-------|-----------|----------------|--------|
| **Database** | EF Core Interceptor + Specification | `UpdateTenancyInterceptor`, `TenancySpecification<T>` | ✅ Auto |
| **Vector DB** | Metadata filtering | `tenant_id` field trong Qdrant filter | ✅ Manual |
| **Object Storage** | Path prefix | Directory-based isolation trong MinIO | ✅ Manual |
| **API** | JWT claims extraction | `CurrentUserProvider`, `CurrentTenantProvider` | ✅ Auto |
| **Background Jobs** | Manual tenant injection | `SetTenantId()` trong RabbitMQ consumer | ✅ Manual |

**Postcondition:**
- Tenant A không bao giờ thấy dữ liệu của Tenant B (ngoại trừ TenantId = 1 global data)
- TenantId được auto-inject khi tạo mới entity
- Mọi query đều auto-filter theo TenantId
- Background processes (RabbitMQ) có tenant context từ JWT token

**Key Implementation Details:**
- **Shared Infrastructure:** Single database, single Qdrant collection, single MinIO bucket
- **Row-level Isolation:** Qua TenantId column trong database và metadata trong Qdrant
- **Global Tenant (TenantId = 1):** Luật quốc gia accessible cho tất cả tenants
- **Hybrid Tenant Provider:** Hỗ trợ cả HTTP requests và background processes
- **No Bucket/Collection per Tenant:** Không tạo riêng collection/bucket cho mỗi tenant

---

### 2.3.4. UC4: Internal Entity & Key-Value Mapping (Business Dictionary)

**Tên:** Cấu hình ánh xạ thuật ngữ nội bộ

**Actor:** Tenant Admin

**Mục tiêu:** Lưu trữ các cặp key-value để ánh xạ thuật ngữ viết tắt hoặc thuật ngữ chuyên ngành nội bộ sang định nghĩa đầy đủ, giúp AI hiểu đúng ngữ cảnh tổ chức.

**Phạm vi (Simplified Scope):**
- **KHÔNG phải** system prompt configuration (không customize AI role)
- **CHỈ** là simple key-value mapping cho organizational context
- Ví dụ:
  - "OT" → "Làm thêm giờ (Overtime)"
  - "Mr. A" → "Tổng Giám đốc - Nguyễn Văn A"
  - "Phòng HCNS" → "Phòng Hành chính Nhân sự"

**Precondition:**
- Tenant Admin đã đăng nhập
- Tenant đã được provisioned

**Main Flow:**

1. **Tenant Admin truy cập "Business Dictionary" page**
   - URL: `/admin/prompt-config`
   - View list các terms đã cấu hình

2. **Tạo term mapping mới:**
   - Nhấn "Add Term"
   - Form nhập:
     ```
     Key: OT
     Value: Làm thêm giờ (Overtime)
     ```
   - Nhấn "Save"

3. **Backend xử lý (C#):**
   - Class: `PromptConfigBusiness.CreateAsync()`
   - Location: `Services\ChatService\Features\PromptConfigBusiness.cs`
   - Check duplicate:
     ```csharp
     var existing = await _repo.FirstOrDefaultAsync(
       new PromptConfigByKeySpec(request.Key, _currentUserProvider.TenantId)
     );
     if (existing != null)
       throw new BusinessException("Term already exists");
     ```
   - Tạo `PromptConfig` entity:
     ```csharp
     var promptConfig = new PromptConfig
     {
       Key = request.Key,
       Value = request.Value,
       TenantId = _currentUserProvider.TenantId  // Auto-injected by interceptor
     };
     await _repo.AddAsync(promptConfig);
     ```

4. **Lưu vào database:**
   - Table: `PromptConfig`
   - Schema:
     ```sql
     CREATE TABLE PromptConfig (
       Id INT PRIMARY KEY,
       Key NVARCHAR(255),
       Value NVARCHAR(MAX),
       TenantId INT,
       CreatedAt DATETIME,
       IsDeleted BIT
     )
     ```

**Sử dụng trong RAG Flow:**

**Phase 1: Query Expansion (Runtime)**
- Khi user gửi message, `ChatBusiness` lookup terms:
  ```csharp
  // Services\ChatService\Features\ChatBusiness.cs
  var promptConfigSpec = new PromptConfigByMessageSpec(request.Message, tenantId);
  var promptConfigs = await _promptConfigRepo.ListAsync(promptConfigSpec);

  // PromptConfigByMessageSpec.cs
  Query.Where(x => EF.Functions.Like(message, "%" + x.Key + "%"));
  ```
- Example:
  - User message: "Quy định OT trong công ty?"
  - Matched term: `{Key: "OT", Value: "Làm thêm giờ (Overtime)"}`
  - Include trong `UserPromptReceivedEvent.SystemInstruction`

**Phase 2: Query Expansion (Python ChatProcessor)**
- Hàm: `_expand_query_with_prompt_config()`
- Location: `Services\ChatProcessor\src\business.py:639-682`
- Logic:
  ```python
  enhanced_message = message
  for config_item in system_instruction:
    key = config_item.get('key', '')
    value = config_item.get('value', '')
    if key in enhanced_message:
      enhanced_message = enhanced_message.replace(key, value)

  # Input:  "Quy định OT trong công ty?"
  # Output: "Quy định Làm thêm giờ (Overtime) trong công ty?"
  ```

**Phase 3: Keyword Extraction for BM25**
- Thêm business terms vào keyword list:
  ```python
  # hybrid_search.py:84-92
  if system_instruction:
    for config_item in system_instruction:
      key = config_item.get('key', '')
      if key and key in query:
        keywords.append(key.lower())  # 'ot'
        value = config_item.get('value', '')
        if value:
          keywords.append(value.lower())  # 'làm thêm giờ'
  ```

**Phase 4: Terminology Injection vào LLM Prompt**
- Hàm: `_build_terminology_definitions()`
- Location: `Services\ChatProcessor\src\business.py:684-710`
- Inject vào conversation history:
  ```python
  {
    'role': 'system',
    'content': '''
    THUẬT NGỮ CHUYÊN MÔN (Terminology Definitions):
    - OT: Làm thêm giờ (Overtime)
    - Phòng HCNS: Phòng Hành chính Nhân sự
    '''
  }
  ```

**Alternate Flow:**
- **A1:** Update existing term
  - `PromptConfigBusiness.UpdateAsync(id, newValue)`
  - Soft delete old + create new (hoặc update trực tiếp)

- **A2:** Delete term
  - `PromptConfigBusiness.DeleteAsync(id)`
  - Soft delete: `IsDeleted = true`

**Postcondition:**
- Business Dictionary được áp dụng tự động trong mọi chat query
- Query expansion giúp tăng recall (tìm thấy nhiều chunks liên quan hơn)
- LLM có context về thuật ngữ nội bộ, trả lời chính xác hơn

**Key Implementation Details:**
- **Đơn giản hóa:** KHÔNG có System Prompt customization (đã remove khỏi scope)
- **Chỉ là Key-Value mapping:** Ánh xạ thuật ngữ nội bộ → định nghĩa
- **Áp dụng runtime:** Chỉ trong RAG query phase, KHÔNG lưu trong metadata chunks
- **3 pha xử lý:**
  1. Query expansion (replace abbreviations)
  2. Keyword extraction (boost BM25)
  3. Terminology injection (LLM context)
- **Tenant isolation:** Mỗi tenant có business dictionary riêng

**Use Cases:**
- Ánh xạ tên viết tắt: "OT" → "Overtime"
- Ánh xạ chức danh: "Mr. A" → "Tổng Giám đốc"
- Ánh xạ phòng ban: "Phòng HCNS" → "Phòng Hành chính Nhân sự"
- Ánh xạ thuật ngữ ngành: "COD" → "Cash on Delivery"

---

## 2.4. Yêu cầu phi chức năng

**Mục tiêu:** Đặc tả các yêu cầu về hiệu năng, bảo mật, và khả năng mở rộng.

**Số trang:** 1.5-2 trang

### 2.4.1. Performance Requirements

**NFR-P1: Response Latency**
- **Yêu cầu:** Time to First Token (TTFT) < 3 giây
- **Đo lường:** 
  ```
  TTFT = T(first_token_received) - T(user_submit_query)
  ```
- **Chiến lược đạt được:**
  - Async processing với RabbitMQ (decouple request/response)
  - Qdrant HNSW index cho fast vector search (O(log n))
  - Embedding cache cho popular queries
  - Ollama local deployment (no network latency)

**NFR-P2: Enrichment Processing Speed**
- **Yêu cầu:** 1 document 50 trang phải enriched trong < 5 phút
- **Chiến lược:**
  - Batch embedding (100 chunks/request)
  - Parallel legal linking (concurrent searches)
  - Hangfire background jobs

**NFR-P3: Concurrent Users**
- **Yêu cầu:** Hỗ trợ 50 concurrent chat sessions
- **Chiến lược:**
  - SignalR WebSocket connection pooling
  - RabbitMQ queue buffering
  - Multiple ChatProcessor workers (horizontal scaling)

### 2.4.2. Security & Privacy Requirements

**NFR-S1: Multi-tenant Data Isolation**
- **Yêu cầu:** Zero tolerance cho data leakage giữa tenants
- **Enforcement:**
  - EF Core Global Query Filter (database level)
  - Qdrant collection separation (vector DB level)
  - MinIO bucket isolation (object storage level)
  - Authorization checks (API level)

**NFR-S2: Authentication & Authorization**
- **Yêu cầu:** Mọi endpoint (trừ `/auth/*`) yêu cầu JWT token
- **Implementation:**
  - JWT với expiry 24h
  - Role-based access control (SystemAdmin, TenantAdmin, EndUser)
  - Policy-based authorization cho sensitive operations

**NFR-S3: Password Security**
- **Yêu cầu:** Mật khẩu phải hash bằng BCrypt (cost factor ≥ 10)
- **Implementation:** BCrypt.Net-Next library

**NFR-S4: Data Privacy**
- **Yêu cầu:** Tất cả dữ liệu nhạy cảm phải self-hosted (không cloud)
- **Implementation:**
  - Ollama local (không gọi OpenAI/Claude API)
  - MinIO self-hosted (không AWS S3)
  - Qdrant self-hosted (không Pinecone cloud)

### 2.4.3. Scalability Requirements

**NFR-SC1: Horizontal Scalability**
- **Yêu cầu:** Hỗ trợ 10+ tenants đồng thời
- **Chiến lược:**
  - Row-level tenant isolation (shared database, shared schema)
  - Per-tenant Qdrant collections
  - Stateless microservices (dễ scale out)

**NFR-SC2: Vector Database Capacity**
- **Yêu cầu:** Chứa 100,000+ vectors mà không degraded performance
- **Chiến lược:**
  - Qdrant HNSW index (scalable to millions)
  - Per-tenant sharding nếu cần

**NFR-SC3: Storage Scalability**
- **Yêu cầu:** MinIO phải lưu được 10GB+ documents per tenant
- **Chiến lược:**
  - MinIO distributed mode (nếu cần)
  - Object lifecycle policies (auto-delete old versions)

### 2.4.4. Reliability Requirements

**NFR-R1: Fault Tolerance**
- **Yêu cầu:** 1 service down không làm sập toàn hệ thống
- **Chiến lược:**
  - Circuit Breaker pattern (Polly)
  - Health checks cho mỗi service
  - Graceful degradation (ví dụ: nếu Ollama down, hiển thị "AI unavailable" nhưng vẫn xem được history)

**NFR-R2: Retry Mechanism**
- **Yêu cầu:** Transient errors phải retry tự động
- **Implementation:**
  - RabbitMQ MassTransit retry: 3 lần, exponential backoff
  - HTTP retry với Polly: 3 lần, 2^n seconds delay

**NFR-R3: Data Backup**
- **Yêu cầu:** Database backup hàng ngày
- **Implementation:**
  - SQL Server automated backup
  - MinIO object versioning (30 versions retained)

### 2.4.5. Maintainability Requirements

**NFR-M1: Code Quality**
- **Yêu cầu:** Tuân thủ SOLID principles
- **Implementation:**
  - Repository + Specification pattern
  - Dependency Injection
  - Clear separation of concerns (microservices)

**NFR-M2: Logging & Observability**
- **Yêu cầu:** Mọi error phải được log với đầy đủ context
- **Implementation:**
  - Serilog structured logging
  - Centralized logs (tất cả services log vào một nơi)
  - Log levels: Debug, Info, Warning, Error, Fatal

**NFR-M3: Configuration Management**
- **Yêu cầu:** Config dễ thay đổi mà không rebuild
- **Implementation:**
  - appsettings.json per environment
  - Environment variables trong Docker Compose
  - Secrets management (không commit secrets vào Git)

---

## 2.5. Ràng buộc và giả định

**Số trang:** 0.5 trang

### 2.5.1. Ràng buộc (Constraints)

**Công nghệ:**
- Backend: .NET 9, Python 3.11+
- Database: SQL Server 2022 (Developer Edition)
- Deployment: Docker Compose (local only, không cloud)

**Thời gian:**
- Phát triển: 4 tháng (1 học kỳ)
- Impact: Không có comprehensive unit tests, chỉ test critical paths

**Ngân sách:**
- Zero budget (không cloud, không API keys)
- Impact: Phải dùng Ollama local (không OpenAI), self-hosted (không AWS)

**Dữ liệu:**
- Phạm vi: Pháp luật lao động Việt Nam
- Impact: Không có production data, chỉ demo data

**Phạm vi:**
- Web app only (không mobile)
- Local deployment (không production)

### 2.5.2. Giả định (Assumptions)

**A1:** Users có mạng ổn định (cho SignalR real-time)

**A2:** Documents upload có cấu trúc rõ ràng (Heading styles đúng)

**A3:** Vistral 7B đủ tốt cho tiếng Việt pháp lý

**A4:** Tenants không cần real-time collaboration (không có conflict resolution cho concurrent edits)

---

## 2.6. Tổng kết chương

**Số trang:** 0.5 trang

**Những điểm chính đã trình bày:**

1. ✅ **Khảo sát hiện trạng:** Phân tích 3 nhóm hệ thống (ai.phapluat.gov.vn, ailuat.luatvietnam.vn, ChatGPT/Gemini/Claude), xác định 4 khoảng trống nghiên cứu chính về Legal Knowledge Enrichment, Business Dictionary, Multi-tenant Isolation, và Hierarchical Knowledge Architecture.

2. ✅ **Tổng quan chức năng:** Định nghĩa 3 actors (với Tenant Admin là Power User có khả năng cấu hình Business Dictionary và System Prompt), phân nhóm 4 functional modules, và mô hình hóa 2 business processes cốt lõi (Legal Knowledge Enrichment Pipeline và Enriched RAG Query Flow).

3. ✅ **Đặc tả chức năng:** Chi tiết 4 use cases quan trọng nhất: (UC1) Business Dictionary & System Prompt Configuration, (UC2) Hierarchical Knowledge Enrichment Processing, (UC3) Enriched RAG Chat, và (UC4) Tenant-level Access Control.

4. ✅ **Yêu cầu phi chức năng:** Đặc tả 5 nhóm NFR về Performance (TTFT < 3s), Security (multi-tenant isolation), Scalability (10+ tenants, 100k+ vectors), Reliability (fault tolerance), và Maintainability (SOLID principles).

5. ✅ **Ràng buộc và giả định:** Liệt kê constraints về công nghệ (.NET 9, Docker Compose), thời gian (4 tháng), ngân sách (zero), và phạm vi (local deployment).

**Khoảng trống nghiên cứu (Research Gaps) đã xác định:**

AIChat2025 lấp đầy 4 gaps chính:
- **Gap 1:** Legal Knowledge Enrichment - Làm giàu tri thức bằng cách kết nối quy định nội bộ với khung pháp lý quốc gia
- **Gap 2:** Business Dictionary & Domain Customization - Tùy biến theo ngành nghề với ánh xạ thuật ngữ và custom prompts
- **Gap 3:** Multi-tenant Data Isolation - Bảo mật dữ liệu với cô lập toàn diện (database + vector DB + object storage)
- **Gap 4:** Hierarchical Knowledge Architecture - Bảo toàn cấu trúc phân cấp của văn bản pháp luật Việt Nam

**Chuyển tiếp sang Chương 3:**
- Chương 2 đã xác định **yêu cầu** và **khoảng trống** cần giải quyết
- Chương 3 sẽ trình bày **cơ sở lý thuyết** về các công nghệ nền tảng (RAG, LLM, Vector DB, Multi-tenancy, Microservices)
- Chương 4 sẽ trình bày **thiết kế hệ thống** (architecture, database schema, API design)
- Chương 5 sẽ trình bày **các giải pháp kỹ thuật** chi tiết (implementation của Legal Knowledge Enrichment, Hierarchical Chunking, Business Dictionary, Dual-RAG)

---

**KẾT THÚC CHƯƠNG 2**

**Điểm nhấn chính của outline mới:**
- ✅ Tập trung vào **Legal Knowledge Enrichment** (không chỉ là comparison)
- ✅ Thu gọn xuống **9-11 trang** (thay vì 18-22 trang)
- ✅ So sánh với **ai.phapluat.gov.vn** và **ailuat.luatvietnam.vn** (hệ thống Việt Nam thực tế)
- ✅ Tenant Admin vừa quản trị vừa chat (**Power User**)
- ✅ Nhấn mạnh **Business Dictionary** và **Custom System Prompts**
- ✅ Chỉ 4 use cases chính (thay vì 7), tập trung vào tính năng độc đáo
- ✅ Mô tả chi tiết **Enrichment Pipeline** (hierarchical chunking + legal linking + business term mapping)
