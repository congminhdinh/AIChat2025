# CHƯƠNG 4: THIẾT KẾ GIẢI PHÁP

## 4.1. Tổng quan kiến trúc hệ thống
(Nội dung sẽ được bổ sung)

## 4.2. Thiết kế chi tiết

### 4.2.1. Thiết kế cơ sở dữ liệu
(Nội dung sẽ được bổ sung)

### 4.2.2. Thiết kế các lớp (Class Design)

Phần này tập trung vào thiết kế chi tiết của 4 lớp nghiệp vụ chính (Business Logic Classes) trong 2 luồng nghiệp vụ quan trọng nhất của hệ thống RAG pháp lý.

#### A. Đặc tả lớp chi tiết (Detailed Class Specification)

##### 1. PromptDocumentBusiness (.NET - DocumentService)

**Mô tả:** Lớp nghiệp vụ chịu trách nhiệm xử lý tài liệu pháp lý từ khi tải lên, chuẩn hóa định dạng, chia nhỏ theo cấu trúc phân cấp (Hierarchical Chunking), và tạo job vectorization qua Hangfire.

**Thuộc tính chính (Key Attributes):**
- `_documentRepository: IRepository<PromptDocument>` - Repository quản lý tài liệu
- `_currentUserProvider: ICurrentUserProvider` - Cung cấp thông tin người dùng và tenant
- `_backgroundJobClient: IBackgroundJobClient` - Client để tạo background job (Hangfire)
- `_regexHeading1, _regexHeading2, _regexHeading3: Regex` - Pattern nhận diện heading cấp độ 1, 2, 3 (Chương, Mục, Điều)

**Phương thức chính (Key Methods):**
- `CreateDocument(CreateDocumentRequest input): Task<BaseResponse<int>>`
  - Nhận file docx từ client, tạo entity PromptDocument
  - Gọi `StandardizeHeadings()` để chuẩn hóa định dạng heading
  - Upload file lên MinIO Storage qua API Gateway
  - Cập nhật FilePath và trạng thái `DocumentAction.Upload`

- `VectorizeDocument(VectorizeDocumentRequest input): Task<BaseResponse<bool>>`
  - Download file từ MinIO Storage
  - Gọi `ExtractHierarchicalChunks()` để chia nhỏ tài liệu
  - Chia chunks thành batch (mỗi batch 10 chunks)
  - Tạo Hangfire background job cho mỗi batch qua `_backgroundJobClient.Enqueue<VectorizeBackgroundJob>()`
  - Cập nhật trạng thái `DocumentAction.Vectorize_Success`

- `ExtractHierarchicalChunks(Stream stream, int documentId, ...): List<DocumentChunkDto>`
  - Đọc file Word (.docx) bằng DocumentFormat.OpenXml
  - Duyệt qua các paragraph, nhận diện heading bằng regex
  - Gom nội dung theo cấu trúc: Heading1 (Chương) → Heading2 (Mục) → Heading3 (Điều) → Content
  - Gọi `FlushChunk()` để tạo DocumentChunkDto chứa: Heading1, Heading2, Content, FullText, Metadata

- `DeleteDocument(DeleteDocumentRequest input): Task<BaseResponse<bool>>`
  - Soft delete từ SQL database
  - Gọi Embedding Service API `/api/embeddings/delete` để xóa vectors trong Qdrant
  - Xử lý lỗi riêng biệt cho vector deletion (soft consistency)

##### 2. VectorizeBackgroundJob (.NET - DocumentService)

**Mô tả:** Lớp xử lý background job vectorization qua Hangfire. Nhận batch chunks, gọi Embedding Service để vectorize và lưu vào Qdrant.

**Thuộc tính chính:**
- `_appSettings: AppSettings` - Cấu hình URL của Embedding Service
- `_logger: IAppLogger<BaseHttpClient>` - Logger để ghi nhận log

**Phương thức chính:**
- `ProcessBatch(List<DocumentChunkDto> chunks, int tenantId): Task`
  - Nhận batch chunks từ Hangfire
  - Chuẩn bị `BatchVectorizeRequestDto` với metadata (source_id, file_name, heading1, heading2, content, tenant_id, type)
  - POST request đến `{EmbeddingServiceUrl}/vectorize-batch`
  - Nhận response và ghi log thành công/thất bại

##### 3. ChatHub (.NET - ChatService)

**Mô tả:** SignalR Hub quản lý kết nối real-time giữa client và server. Xử lý nhận message từ user, publish vào RabbitMQ, và broadcast response từ bot về client.

**Thuộc tính chính:**
- `chatBusiness: ChatBusiness` - Lớp business logic xử lý chat
- `logger: ILogger<ChatHub>` - Logger

**Phương thức chính:**
- `SendMessage(int conversationId, string message, int userId): Task`
  - Nhận message từ SignalR client
  - Gọi `chatBusiness.SaveUserMessageAndPublishAsync()` để lưu message vào DB và publish vào RabbitMQ
  - Broadcast message đến tất cả clients trong group `conversation-{conversationId}` qua method `ReceiveMessage`

- `JoinConversation(int conversationId): Task`
  - Thêm client vào SignalR group `conversation-{conversationId}`

- `LeaveConversation(int conversationId): Task`
  - Xóa client khỏi SignalR group

- `BroadcastBotResponse(IHubContext<ChatHub> hubContext, int conversationId, object messageDto): Task` (Static)
  - Được gọi bởi `BotResponseConsumer` khi nhận response từ RabbitMQ
  - Broadcast response đến group qua method `BotResponse`

**Tích hợp ChatBusiness (.NET - ChatService):**
- `SaveUserMessageAndPublishAsync(SendMessageRequest request, CancellationToken ct): Task<MessageDto>`
  - Tạo `ChatMessage` entity với `Type = ChatType.Request`
  - Lưu vào database qua `_messageRepo.AddAsync()`
  - Tạo `UserPromptReceivedEvent` chứa: ConversationId, Message, Token, Timestamp, SystemInstruction
  - Publish event vào RabbitMQ qua `_publishEndpoint.Publish(userPromptEvent)`

##### 4. ChatBusiness (Python - ChatProcessor) ⭐ QUAN TRỌNG NHẤT

**Mô tả:** Lớp nghiệp vụ chính của Python Worker, chịu trách nhiệm xử lý RAG workflow: nhận message từ RabbitMQ, thực hiện Hybrid Search, phát hiện scenario, và sinh câu trả lời từ LLM.

**Thuộc tính chính:** (Static class, không có instance attributes)

**Phương thức chính:**

- `process_chat_message(conversation_id, user_id, message, tenant_id, ollama_service, qdrant_service, system_instruction): Dict[str, Any]` (Static, Async)

  **Luồng xử lý chính:**

  1. **Query Expansion (Mở rộng truy vấn):**
     - Gọi `_expand_query_with_prompt_config(message, system_instruction)`
     - Thay thế các key (ví dụ: "OT") bằng giá trị mô tả ("Overtime Payment") từ `system_instruction`
     - Trả về `enhanced_message` với nghĩa đầy đủ

  2. **Legal Term Extraction (Trích xuất từ khóa pháp lý):**
     - Gọi `LegalTermExtractor.extract_keywords(enhanced_message, system_instruction)`
     - Trích xuất keywords: số điều, mã luật, nghị định, thông tư, năm, viết tắt
     - Trả về `legal_keywords: List[str]` cho BM25 matching

  3. **Hybrid Search with Fallback:**
     - Embedding query: `query_embedding = await qdrant_service.get_embedding(enhanced_message)`
     - Hybrid search: `company_rule_results, legal_base_results, fallback_triggered = await qdrant_service.hybrid_search_with_fallback(query_vector, keywords, tenant_id, limit=5)`
     - Kết hợp Vector Search + Keyword Search + RRF Fusion
     - Nếu tenant thiếu tài liệu → fallback sang global legal docs (tenant_id=1)

  4. **Scenario Detection (Phát hiện kịch bản):**
     - Gọi `_detect_scenario(company_rule_results, legal_base_results)`
     - Trả về: "BOTH", "COMPANY_ONLY", "LEGAL_ONLY", hoặc "NONE"
     - Nếu "NONE" → trả lỗi ngay, không gọi LLM

  5. **Context Structuring (Cấu trúc ngữ cảnh):**
     - Gọi `_structure_context_for_compliance(company_rule_results, legal_base_results, tenant_id, scenario)`
     - Tạo context string với 2 nhóm: "NỘI QUY CÔNG TY" và "VĂN BẢN PHÁP LUẬT"
     - Mỗi chunk có citation label: `[Nội quy: Tên tài liệu - Điều X]`

  6. **System Prompt Selection (Chọn system prompt):**
     - Nếu scenario = "BOTH": gọi `_build_comparison_system_prompt(fallback_triggered)`
     - Nếu scenario = "COMPANY_ONLY" hoặc "LEGAL_ONLY": gọi `_build_single_source_system_prompt(fallback_triggered)`

  7. **LLM Generation (Sinh câu trả lời):**
     - Chuẩn bị conversation history với system prompt và terminology definitions
     - Gọi `ollama_service.generate_response(enhanced_prompt, conversation_history, temperature=0.1)`
     - Temperature thấp (0.1) để giảm hallucination

  8. **Response Cleanup (Làm sạch response):**
     - Gọi `_cleanup_response(ai_response)`
     - Loại bỏ prefix ("Trả lời:", "Câu trả lời:"), reasoning steps ("Bước 1:", "Bước 2:")

  9. **Evaluation Logging (Ghi log đánh giá):**
     - Gọi `evaluation_logger.log_interaction_async()` (fire-and-forget, không chặn response)

  10. **Return Response:**
      - Trả về dict chứa: conversation_id, message, timestamp, model_used, rag_documents_used, source_ids, scenario, fallback_triggered

**Phương thức hỗ trợ:**

- `_detect_scenario(company_rule_results, legal_base_results): str` (Static)
  - Logic: if both → "BOTH", if company only → "COMPANY_ONLY", if legal only → "LEGAL_ONLY", else "NONE"

- `_build_comparison_system_prompt(fallback_mode): str` (Static)
  - System prompt cho scenario "BOTH" (so sánh nội quy công ty với luật)
  - Chứa hard constraints: cấm in "Bước 1, 2, 3", cấm dài dòng, chỉ 1-2 câu
  - Output format: "Theo [Tài liệu X - Điều Y], công ty quy định [số liệu], [đánh giá] mức tối thiểu [số liệu] quy định tại [Tài liệu Z - Điều W]"

- `_build_single_source_system_prompt(fallback_mode): str` (Static)
  - System prompt cho scenario "COMPANY_ONLY" hoặc "LEGAL_ONLY"
  - Lightweight, output format: "Theo [Tài liệu - Điều X], [nội dung]"

- `_cleanup_response(response): str` (Static)
  - Loại bỏ reasoning steps (split by "Bước \d+:", lấy phần cuối)
  - Loại bỏ Vietnamese prefixes ("Trả lời:", "Câu trả lời:", "Dựa trên", v.v.)

- `_expand_query_with_prompt_config(raw_message, prompt_config): str` (Static)
  - Query expansion: thay key → value từ prompt_config

- `_build_terminology_definitions(prompt_config): str` (Static)
  - Tạo section "THUẬT NGỮ CHUYÊN MÔN" để inject vào system prompt

- `_build_citation_label(result, is_company_rule, index): str` (Static)
  - Tạo citation label từ metadata: `[Nội quy: Tên tài liệu - Điều X]`

- `_structure_context_for_compliance(company_rule_results, legal_base_results, tenant_id, scenario): tuple[str, list, int]` (Static)
  - Cấu trúc context string với delimiter rõ ràng, citation labels, return (context_string, source_ids, documents_count)

**Tích hợp QdrantService (Python - ChatProcessor):**
- `hybrid_search_with_fallback(query_vector, keywords, tenant_id, limit): tuple[List, List, bool]` (Async)
  - Parallel search: tenant hybrid search + global hybrid search
  - Mỗi hybrid search = Vector Search + Keyword Search + RRF Fusion
  - Apply fallback logic: nếu tenant results < 2 quality results → prioritize global
  - Return: (tenant_results, global_results, fallback_triggered)

- `hybrid_search_single_tenant(query_vector, keywords, tenant_id, limit): List[ScoredPoint]` (Async)
  - Vector search: `search_exact_tenant(query_vector, tenant_id, limit*2)`
  - Keyword search: `search_with_keywords(query_vector, keywords, tenant_id, limit*2)`
  - RRF fusion: `ReciprocalRankFusion.fuse(vector_results, keyword_results, k=60)`

**Tích hợp RabbitMQService (Python - ChatProcessor):**
- `consume_messages(message_handler): None` (Async)
  - Lắng nghe queue `user_prompts`
  - Parse MassTransit envelope, extract `UserPromptReceivedMessage`
  - Gọi `message_handler(prompt_message)`

- `publish_response(response: BotResponseCreatedMessage): None` (Async)
  - Wrap response vào MassTransit envelope
  - Publish vào queue `bot_responses`

#### B. Thiết kế luồng thông điệp (Message Flow Design)

##### 1. Use Case 1: Document Processing (Chunking & Embedding)

**Mô tả luồng:**
1. Client upload file .docx qua `DocumentEndpoint.CreateDocument()`
2. `PromptDocumentBusiness.CreateDocument()` nhận request:
   - Tạo entity `PromptDocument` với `Action = DocumentAction.Upload`
   - Gọi `StandardizeHeadings(memoryStream)` để chuẩn hóa heading trong Word document
   - Upload file lên MinIO qua `StorageService.UploadFile()` (HTTP POST)
   - Cập nhật `FilePath` và lưu vào database
3. Client gọi `DocumentEndpoint.VectorizeDocument(documentId)`
4. `PromptDocumentBusiness.VectorizeDocument()`:
   - Download file từ MinIO qua `StorageService.DownloadFile()` (HTTP GET)
   - Gọi `ExtractHierarchicalChunks()`:
     * Mở Word document bằng `WordprocessingDocument.Open()`
     * Duyệt qua từng `Paragraph`, nhận diện heading bằng `_regexHeading1/2/3.IsMatch()`
     * Gom content theo cấu trúc phân cấp
     * Gọi `FlushChunk()` để tạo `DocumentChunkDto`
   - Chia chunks thành batches (10 chunks/batch)
   - Với mỗi batch, gọi `_backgroundJobClient.Enqueue<VectorizeBackgroundJob>(job => job.ProcessBatch(batch, tenantId))`
5. Hangfire Server nhận job, thực thi `VectorizeBackgroundJob.ProcessBatch()`:
   - Chuẩn bị `BatchVectorizeRequestDto` với metadata
   - POST request đến `EmbeddingService.VectorizeBatch()` (Python FastAPI)
   - Embedding Service:
     * Vectorize từng chunk bằng SentenceTransformer
     * Upsert vectors vào Qdrant collection `vn_law_documents`
   - Trả về response `{ success: true }`
6. `VectorizeBackgroundJob` log kết quả

**Sequence Diagram (mô tả):**
```
Client → DocumentEndpoint: POST /api/documents (file)
DocumentEndpoint → PromptDocumentBusiness: CreateDocument(request)
PromptDocumentBusiness → Database: AddAsync(PromptDocument)
PromptDocumentBusiness → PromptDocumentBusiness: StandardizeHeadings(stream)
PromptDocumentBusiness → StorageService: UploadFile(file) [HTTP]
StorageService → MinIO: Upload file
StorageService → PromptDocumentBusiness: return FilePath
PromptDocumentBusiness → Database: UpdateAsync(FilePath)
PromptDocumentBusiness → DocumentEndpoint: return DocumentId
DocumentEndpoint → Client: 200 OK (DocumentId)

Client → DocumentEndpoint: POST /api/documents/vectorize (documentId)
DocumentEndpoint → PromptDocumentBusiness: VectorizeDocument(request)
PromptDocumentBusiness → StorageService: DownloadFile(filePath) [HTTP]
StorageService → MinIO: Download file
StorageService → PromptDocumentBusiness: return FileStream
PromptDocumentBusiness → PromptDocumentBusiness: ExtractHierarchicalChunks(stream)
  Loop: For each paragraph
    PromptDocumentBusiness: Check regex heading
    PromptDocumentBusiness: FlushChunk() → DocumentChunkDto
  End Loop
PromptDocumentBusiness → PromptDocumentBusiness: Split chunks into batches
  Loop: For each batch
    PromptDocumentBusiness → Hangfire: Enqueue(VectorizeBackgroundJob.ProcessBatch)
  End Loop
PromptDocumentBusiness → Database: UpdateAsync(Vectorize_Success)
PromptDocumentBusiness → DocumentEndpoint: return true
DocumentEndpoint → Client: 200 OK

Hangfire → VectorizeBackgroundJob: ProcessBatch(chunks, tenantId)
VectorizeBackgroundJob → EmbeddingService: POST /vectorize-batch [HTTP]
EmbeddingService → SentenceTransformer: Embed(text)
SentenceTransformer → EmbeddingService: return vector
EmbeddingService → Qdrant: Upsert(vector, metadata)
Qdrant → EmbeddingService: OK
EmbeddingService → VectorizeBackgroundJob: return success
VectorizeBackgroundJob → Logger: Log success
```

##### 2. Use Case 2: Legal Q&A (Chat RAG Flow)

**Mô tả luồng:**
1. Client kết nối SignalR Hub:
   - Client gọi `ChatHub.JoinConversation(conversationId)`
   - `ChatHub` thêm client vào group `conversation-{conversationId}`
2. User gửi message:
   - Client gọi `ChatHub.SendMessage(conversationId, message, userId)` qua SignalR
3. `ChatHub.SendMessage()`:
   - Gọi `ChatBusiness.SaveUserMessageAndPublishAsync(request)`
   - `ChatBusiness` (.NET):
     * Tạo `ChatMessage` entity với `Type = ChatType.Request`
     * Lưu vào database qua `_messageRepo.AddAsync()`
     * Query `PromptConfig` từ database theo message keywords
     * Tạo `UserPromptReceivedEvent` chứa: ConversationId, Message, Token, SystemInstruction
     * Publish event vào RabbitMQ qua `_publishEndpoint.Publish(userPromptEvent)`
   - `ChatHub` broadcast message đến all clients trong group qua `Clients.Group().SendAsync("ReceiveMessage", messageDto)`
4. Python Worker (ChatProcessor) nhận message:
   - `RabbitMQService.consume_messages()` lắng nghe queue `user_prompts`
   - Parse MassTransit envelope, extract `UserPromptReceivedMessage`
   - Gọi `message_handler(prompt_message)` → Consumer
5. Consumer gọi `ChatBusiness.process_chat_message()` (Python):
   - **Step 1: Query Expansion**
     * Gọi `_expand_query_with_prompt_config(message, system_instruction)`
     * Thay key → value, trả về `enhanced_message`
   - **Step 2: Legal Term Extraction**
     * Gọi `LegalTermExtractor.extract_keywords(enhanced_message)`
     * Trích xuất legal keywords (điều, luật, nghị định, v.v.)
   - **Step 3: Hybrid Search with Fallback**
     * Gọi `QdrantService.get_embedding(enhanced_message)` → `query_embedding`
     * Gọi `QdrantService.hybrid_search_with_fallback()`:
       - Parallel: `hybrid_search_single_tenant(tenant_id)` + `hybrid_search_single_tenant(tenant_id=1)`
       - Mỗi hybrid search:
         * Vector search: `search_exact_tenant(query_vector, tenant_id)`
         * Keyword search: `search_with_keywords(query_vector, keywords, tenant_id)`
         * RRF fusion: `ReciprocalRankFusion.fuse(vector_results, keyword_results)`
       - Apply fallback logic: `HybridSearchStrategy.apply_fallback_logic()`
     * Trả về: (company_rule_results, legal_base_results, fallback_triggered)
   - **Step 4: Scenario Detection**
     * Gọi `_detect_scenario(company_rule_results, legal_base_results)`
     * Nếu "NONE" → return error response ngay
   - **Step 5: Context Structuring**
     * Gọi `_structure_context_for_compliance()` → context_string với citation labels
   - **Step 6: System Prompt Selection**
     * Nếu "BOTH" → `_build_comparison_system_prompt(fallback_triggered)`
     * Nếu "COMPANY_ONLY"/"LEGAL_ONLY" → `_build_single_source_system_prompt(fallback_triggered)`
   - **Step 7: LLM Generation**
     * Chuẩn bị conversation_history với system prompt + terminology definitions
     * Gọi `OllamaService.generate_response(enhanced_prompt, conversation_history, temperature=0.1)`
     * Ollama LLM (Vistral) sinh câu trả lời
   - **Step 8: Response Cleanup**
     * Gọi `_cleanup_response(ai_response)` để loại bỏ prefix và reasoning steps
   - **Step 9: Evaluation Logging**
     * Gọi `evaluation_logger.log_interaction_async()` (fire-and-forget)
   - Return response dict
6. Consumer publish response:
   - Tạo `BotResponseCreatedMessage` từ response dict
   - Gọi `RabbitMQService.publish_response(bot_response)`
   - Wrap vào MassTransit envelope
   - Publish vào queue `bot_responses`
7. .NET ChatService nhận response:
   - `BotResponseConsumer.Consume(context)` nhận message từ RabbitMQ
   - Gọi `ChatBusiness.SaveBotMessageAsync(botResponse)` (.NET):
     * Decode JWT token để lấy userId, tenantId
     * Tạo `ChatMessage` entity với `Type = ChatType.Response`
     * Lưu vào database
   - Gọi `ChatHub.BroadcastBotResponse(hubContext, conversationId, messageDto)`
   - `ChatHub` broadcast response đến all clients trong group qua `Clients.Group().SendAsync("BotResponse", messageDto)`
8. Client nhận response qua SignalR callback `BotResponse`

**Sequence Diagram (mô tả):**
```
Client → ChatHub: SendMessage(conversationId, message, userId) [SignalR]
ChatHub → ChatBusiness (.NET): SaveUserMessageAndPublishAsync(request)
ChatBusiness → Database: AddAsync(ChatMessage, Type=Request)
ChatBusiness → Database: ListAsync(PromptConfig) [Query system_instruction]
ChatBusiness → RabbitMQ: Publish(UserPromptReceivedEvent)
ChatBusiness → ChatHub: return MessageDto
ChatHub → SignalR Group: SendAsync("ReceiveMessage", messageDto)
SignalR Group → Client: ReceiveMessage callback

RabbitMQ → RabbitMQService (Python): consume_messages()
RabbitMQService → Consumer: message_handler(UserPromptReceivedMessage)
Consumer → ChatBusiness (Python): process_chat_message()
  ChatBusiness → ChatBusiness: _expand_query_with_prompt_config()
  ChatBusiness → LegalTermExtractor: extract_keywords()
  ChatBusiness → QdrantService: get_embedding(enhanced_message)
  QdrantService → EmbeddingService: POST /embed [HTTP]
  EmbeddingService → QdrantService: return vector
  ChatBusiness → QdrantService: hybrid_search_with_fallback()
    QdrantService → QdrantService: hybrid_search_single_tenant(tenant_id)
      QdrantService → Qdrant: search_exact_tenant(vector, tenant_id)
      Qdrant → QdrantService: vector_results
      QdrantService → Qdrant: search_with_keywords(vector, keywords, tenant_id)
      Qdrant → QdrantService: keyword_results
      QdrantService → ReciprocalRankFusion: fuse(vector_results, keyword_results)
      ReciprocalRankFusion → QdrantService: fused_results
    QdrantService → QdrantService: hybrid_search_single_tenant(tenant_id=1)
    QdrantService → HybridSearchStrategy: apply_fallback_logic()
    HybridSearchStrategy → QdrantService: (tenant_results, global_results, fallback)
  QdrantService → ChatBusiness: (company_results, legal_results, fallback)
  ChatBusiness → ChatBusiness: _detect_scenario()
  ChatBusiness → ChatBusiness: _structure_context_for_compliance()
  ChatBusiness → ChatBusiness: _build_comparison_system_prompt() OR _build_single_source_system_prompt()
  ChatBusiness → OllamaService: generate_response(prompt, history, temp=0.1)
  OllamaService → Ollama LLM: POST /api/chat [HTTP]
  Ollama LLM → OllamaService: return ai_response
  OllamaService → ChatBusiness: ai_response
  ChatBusiness → ChatBusiness: _cleanup_response()
  ChatBusiness → EvaluationLogger: log_interaction_async() [async, fire-and-forget]
ChatBusiness → Consumer: return response_dict
Consumer → RabbitMQService: publish_response(BotResponseCreatedMessage)
RabbitMQService → RabbitMQ: Publish(MassTransit envelope)

RabbitMQ → BotResponseConsumer (.NET): Consume(BotResponseCreatedEvent)
BotResponseConsumer → ChatBusiness (.NET): SaveBotMessageAsync(botResponse)
ChatBusiness → Database: AddAsync(ChatMessage, Type=Response)
ChatBusiness → BotResponseConsumer: return MessageDto
BotResponseConsumer → ChatHub: BroadcastBotResponse(hubContext, conversationId, messageDto)
ChatHub → SignalR Group: SendAsync("BotResponse", messageDto)
SignalR Group → Client: BotResponse callback
```

### 4.2.3. Thiết kế giao diện
(Nội dung sẽ được bổ sung)

## 4.3. Đánh giá thiết kế
(Nội dung sẽ được bổ sung)
