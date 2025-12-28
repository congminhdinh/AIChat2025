# PHỤ LỤC B: ĐẶC TẢ USE CASE CHI TIẾT

**Mục đích:** Mô tả chi tiết các use case chính của hệ thống với flow, preconditions, postconditions

**Số trang ước tính:** 8-10 trang

---

## B.1. Tổng quan về các use case

**Nội dung chính:**

### B.1.1. Use Case Diagram

**Actors:**
- **Nhân viên (Employee):** Người dùng cuối, hỏi chatbot về quy định và luật
- **HR/Admin:** Quản trị viên, upload tài liệu, quản lý cấu hình
- **System Admin:** Quản trị hệ thống, quản lý tenants

**Use Cases chính:**
1. UC1: Đăng ký tài khoản
2. UC2: Đăng nhập
3. UC3: Upload tài liệu
4. UC4: Hỏi chatbot (RAG query)
5. UC5: Xem lịch sử chat
6. UC6: Quản lý tenant
7. UC7: Quản lý prompt config

**Use Case Diagram:** `diagrams_to_create.md` → Diagram 1.1 (Use Case Overview)

### B.1.2. Mối quan hệ giữa các use case

**Include:**
- UC4 (Hỏi chatbot) **includes** UC2 (Đăng nhập) - Phải đăng nhập mới chat được

**Extend:**
- UC4 (Hỏi chatbot) **extends** UC8 (Đánh giá câu trả lời) - Optional: User có thể rate response

**Generalization:**
- UC2 (Đăng nhập) **generalizes** UC2a (Đăng nhập web), UC2b (Đăng nhập mobile) - future

---

## B.2. UC1: Đăng ký tài khoản

### B.2.1. Thông tin cơ bản

| Field | Value |
|-------|-------|
| **Use Case ID** | UC1 |
| **Use Case Name** | Đăng ký tài khoản |
| **Actors** | HR/Admin |
| **Preconditions** | - User đã có thông tin về tenant (công ty)<br>- Tenant đã được tạo trong hệ thống |
| **Postconditions** | - Account mới được tạo trong database<br>- User có thể đăng nhập |
| **Priority** | Medium |

### B.2.2. Main Flow (Luồng chính)

**Steps:**
1. HR/Admin truy cập trang đăng ký
2. Hệ thống hiển thị form đăng ký:
   - Email (required)
   - Mật khẩu (required, min 8 ký tự)
   - Xác nhận mật khẩu (required)
   - Họ tên (required)
   - Công ty (dropdown - chọn từ danh sách tenants)
3. HR/Admin nhập thông tin và click "Đăng ký"
4. Hệ thống validate input:
   - Email đúng format
   - Mật khẩu >= 8 ký tự
   - Mật khẩu khớp với xác nhận
   - Email chưa tồn tại trong tenant đó
5. Hệ thống hash password với BCrypt
6. Hệ thống lưu account vào database với:
   - TenantId (từ dropdown)
   - Email
   - PasswordHash
   - FullName
   - Role = 'user' (default)
   - IsActive = true
7. Hệ thống hiển thị thông báo thành công
8. Redirect về trang đăng nhập

### B.2.3. Alternative Flows (Luồng thay thế)

**AF1: Email đã tồn tại**
- Bước 4: Hệ thống phát hiện email đã tồn tại trong tenant
- Hệ thống hiển thị lỗi: "Email đã được sử dụng"
- Quay về bước 2

**AF2: Mật khẩu không đủ mạnh**
- Bước 4: Password < 8 ký tự
- Hệ thống hiển thị lỗi: "Mật khẩu phải có ít nhất 8 ký tự"
- Quay về bước 2

**AF3: Mật khẩu không khớp**
- Bước 4: Password != Confirm Password
- Hệ thống hiển thị lỗi: "Mật khẩu xác nhận không khớp"
- Quay về bước 2

### B.2.4. Exception Flows (Luồng ngoại lệ)

**EF1: Database connection error**
- Bước 6: Database không kết nối được
- Hệ thống hiển thị lỗi: "Lỗi hệ thống, vui lòng thử lại sau"
- Log error vào Serilog
- Quay về bước 2

### B.2.5. Business Rules

**BR1:** Email phải unique trong phạm vi tenant (không phải global unique)
**BR2:** Password phải hash với BCrypt (work factor = 12)
**BR3:** Default role = 'user', admin phải được assign thủ công

---

## B.3. UC2: Đăng nhập

### B.3.1. Thông tin cơ bản

| Field | Value |
|-------|-------|
| **Use Case ID** | UC2 |
| **Use Case Name** | Đăng nhập |
| **Actors** | Nhân viên, HR/Admin |
| **Preconditions** | - User đã có tài khoản<br>- Account IsActive = true |
| **Postconditions** | - User được authenticated<br>- JWT token được tạo<br>- Cookie được set (web)<br>- Redirect về trang chủ |
| **Priority** | High |

### B.3.2. Main Flow

**Steps:**
1. User truy cập trang đăng nhập
2. Hệ thống hiển thị form:
   - Email (required)
   - Password (required)
   - "Remember me" checkbox (optional)
3. User nhập email, password và click "Đăng nhập"
4. Hệ thống validate input (not empty)
5. Hệ thống tìm Account bằng email
6. Hệ thống verify password với BCrypt
7. Hệ thống generate JWT token với claims:
   - UserId
   - TenantId
   - Email
   - Role
   - Scope (scope_web hoặc scope_mobile)
   - Expiry (24 giờ)
8. **Web:** Hệ thống set cookie HttpOnly, Secure
9. **Mobile (future):** Return JWT token trong response JSON
10. Hệ thống log "User {userId} logged in"
11. Redirect về trang chat

### B.3.3. Alternative Flows

**AF1: Email không tồn tại**
- Bước 5: Không tìm thấy account với email đó
- Hệ thống hiển thị lỗi: "Email hoặc mật khẩu không đúng" (không nên reveal email không tồn tại)
- Quay về bước 2

**AF2: Password sai**
- Bước 6: BCrypt verify failed
- Hệ thống hiển thị lỗi: "Email hoặc mật khẩu không đúng"
- Quay về bước 2

**AF3: Account bị vô hiệu hóa**
- Bước 5: Account.IsActive = false
- Hệ thống hiển thị lỗi: "Tài khoản đã bị khóa, vui lòng liên hệ admin"
- Quay về bước 2

### B.3.4. Exception Flows

**EF1: JWT secret key not configured**
- Bước 7: JwtSecretKey = null
- Hệ thống throw exception
- Log critical error
- Hiển thị: "Lỗi cấu hình hệ thống"

### B.3.5. Security Considerations

**SC1:** Rate limiting (future): Chỉ cho phép 5 login attempts / 5 phút / IP
**SC2:** Account lockout (future): Khóa account sau 10 failed attempts
**SC3:** Password không được log hoặc hiển thị trong error messages

**Sequence Diagram:** `diagrams_to_create.md` → Diagram 4.5 (Authentication Sequence)

---

## B.4. UC3: Upload tài liệu

### B.4.1. Thông tin cơ bản

| Field | Value |
|-------|-------|
| **Use Case ID** | UC3 |
| **Use Case Name** | Upload tài liệu |
| **Actors** | HR/Admin |
| **Preconditions** | - User đã đăng nhập<br>- User có role 'admin'<br>- File .docx hợp lệ |
| **Postconditions** | - File được lưu vào MinIO<br>- PromptDocument record được tạo<br>- Background job được enqueue<br>- Vectors được lưu vào Qdrant (sau khi job hoàn thành) |
| **Priority** | High |

### B.4.2. Main Flow

**Steps:**
1. HR/Admin truy cập trang "Document Management"
2. Hệ thống hiển thị:
   - Danh sách tài liệu đã upload (của tenant)
   - Button "Upload New Document"
3. HR/Admin click "Upload New Document"
4. Hệ thống hiển thị form:
   - Tên tài liệu (required)
   - Chọn file .docx (required)
   - Loại tài liệu: "Quy định công ty" / "Luật nhà nước" (radio button)
5. HR/Admin nhập thông tin, chọn file, click "Upload"
6. Hệ thống validate:
   - File extension = .docx
   - File size <= 10MB
   - Tên tài liệu không rỗng
7. **DocumentService:** Gọi StorageService API để upload file vào MinIO
8. **StorageService:** Lưu file vào MinIO bucket `ai-chat-2025/tenant_{tenantId}/{fileName}`
9. **StorageService:** Return fileName về DocumentService
10. **DocumentService:** Tạo PromptDocument record:
    - TenantId (từ CurrentTenantProvider)
    - DocumentName (từ form)
    - FileName (từ StorageService)
    - Status = 'Pending'
    - IsCompanyRule (từ radio button)
11. **DocumentService:** Enqueue Hangfire job `VectorizeBackgroundJob` với documentId
12. Hệ thống hiển thị thông báo: "Tài liệu đã được upload. Đang xử lý..."
13. Redirect về trang document list

**Background Job (async):**
14. **VectorizeBackgroundJob:** Update Status = 'Processing'
15. Download file từ MinIO
16. Parse .docx với DocumentFormat.OpenXml
17. Hierarchical chunking (preserve Chương/Điều structure)
18. Gọi EmbeddingService API `/vectorize-batch` với array of chunks
19. **EmbeddingService:** Embed chunks → Store vectors vào Qdrant với payload:
    - text, document_name, heading1, heading2, tenant_id
20. **VectorizeBackgroundJob:** Update Status = 'Completed'
21. (Optional) Gửi notification đến admin

### B.4.3. Alternative Flows

**AF1: File không phải .docx**
- Bước 6: File extension != .docx
- Hệ thống hiển thị lỗi: "Chỉ hỗ trợ file .docx"
- Quay về bước 4

**AF2: File quá lớn**
- Bước 6: File size > 10MB
- Hệ thống hiển thị lỗi: "File không được vượt quá 10MB"
- Quay về bước 4

**AF3: MinIO upload failed**
- Bước 8: MinIO connection error
- Hệ thống hiển thị lỗi: "Không thể upload file, vui lòng thử lại"
- Log error
- Quay về bước 4

### B.4.4. Exception Flows

**EF1: Background job failed**
- Bước 16: Parse .docx failed (corrupted file)
- Job update Status = 'Failed'
- Log error với stack trace
- Hangfire auto-retry (max 3 attempts)
- Admin có thể xem error trong Hangfire dashboard

**EF2: EmbeddingService unavailable**
- Bước 18: EmbeddingService API timeout
- Hangfire retry sau 5 phút
- Nếu vẫn fail sau 3 attempts → Status = 'Failed'

### B.4.5. Business Rules

**BR1:** Legal base documents (Luật nhà nước) phải có `IsCompanyRule = false` và `TenantId = 1` (shared)
**BR2:** Company rules phải có `IsCompanyRule = true` và `TenantId = {specific tenant}` (isolated)
**BR3:** Chunking phải preserve hierarchy: `document_name` → `heading1` (Chương) → `heading2` (Điều)
**BR4:** Metadata phải đầy đủ để trích dẫn chính xác

**Sequence Diagram:** `diagrams_to_create.md` → Diagram 4.6 (Document Embedding Sequence)

---

## B.5. UC4: Hỏi chatbot (RAG Query)

### B.5.1. Thông tin cơ bản

| Field | Value |
|-------|-------|
| **Use Case ID** | UC4 |
| **Use Case Name** | Hỏi chatbot |
| **Actors** | Nhân viên |
| **Preconditions** | - User đã đăng nhập<br>- Đã có ít nhất 1 conversation<br>- SignalR connection established |
| **Postconditions** | - User message được lưu vào database<br>- Bot response được lưu vào database<br>- User nhận được câu trả lời real-time |
| **Priority** | High |

### B.5.2. Main Flow

**Steps:**

**[Frontend - User Input]**
1. User gõ câu hỏi vào input box: "Thời gian thử việc là bao lâu?"
2. User nhấn Enter hoặc click "Send"
3. Frontend hiển thị user message ngay lập tức (optimistic update)
4. Frontend hiển thị typing indicator ("Bot đang trả lời...")
5. Frontend gọi SignalR: `connection.invoke("SendMessage", conversationId, userMessage)`

**[ChatService - Orchestration]**
6. **ChatHub:** Nhận message từ SignalR client
7. **ChatHub:** Validate conversationId thuộc về user (TenantId + UserId match)
8. **ChatBusiness:** Lưu user message vào ChatMessages table:
   - ConversationId
   - IsBot = false
   - Content = userMessage
9. **ChatBusiness:** Publish RabbitMQ event `UserPromptReceivedEvent`:
   - ConversationId
   - Message
   - TenantId
   - UserId
10. **ChatHub:** Return success về frontend
11. **Frontend:** Ẩn typing indicator tạm thời (sẽ hiện lại khi ChatProcessor bắt đầu)

**[ChatProcessor - RAG Pipeline]** (Python, async)
12. **RabbitMQ Consumer:** Nhận event `UserPromptReceivedEvent`
13. **ChatBusiness:** Extract tenantId, message
14. **Step 1 - Embedding:** Gọi embedding model để vector hóa query → `query_vector` (768-dim)
15. **Step 2 - Dual-RAG Search:**
    - Search trong company rules: `qdrant.search(query_vector, filter: tenant_id={tenantId})`
    - Search trong legal base: `qdrant.search(query_vector, filter: tenant_id=1)`
    - Lấy top-5 results mỗi collection
16. **Step 3 - Scenario Determination:**
    - Nếu chỉ có company results → `COMPANY_ONLY`
    - Nếu chỉ có legal results → `LEGAL_ONLY`
    - Nếu có cả 2 → `COMPARISON`
17. **Step 4 - Context Structuring:**
    - Extract text + metadata (document_name, heading1, heading2)
    - Build citation labels: `[Bộ luật Lao động 2019 - Chương XV - Điều 24]`
    - Format context với separators
18. **Step 5 - Prompt Construction:**
    - System prompt (based on scenario)
    - Context string
    - User query
19. **Step 6 - LLM Generation:**
    - Gọi Ollama API: `POST http://ollama:11434/api/generate`
    - Model: `ontocord/vistral:latest`
    - Prompt: system + context + query
    - Receive response (streaming hoặc non-streaming)
20. **Step 7 - Response Cleanup:**
    - Remove instruction leakage prefixes
    - Multi-pass cleanup (up to 5 iterations)
21. **Step 8 - Publish Response:**
    - Publish RabbitMQ event `BotResponseCreatedEvent`:
      - ConversationId
      - Response
      - TenantId
22. **Step 9 - Log RAGAS Metrics:** (async, không block)
    - Calculate faithfulness, answer relevancy, context recall, context precision
    - Log metrics vào file

**[ChatService - Response Handling]**
23. **BotResponseConsumer:** Nhận event `BotResponseCreatedEvent`
24. **BotResponseConsumer:** Lưu bot response vào ChatMessages table:
    - ConversationId
    - IsBot = true
    - Content = response
25. **BotResponseConsumer:** Broadcast qua SignalR:
    - `await Clients.Group(conversationId).SendAsync("BotResponse", message)`

**[Frontend - Display Response]**
26. **SignalR Client:** Nhận event "BotResponse"
27. Frontend ẩn typing indicator
28. Frontend hiển thị bot response trong chat area
29. Frontend scroll xuống cuối conversation

### B.5.3. Alternative Flows

**AF1: Query về quy định công ty (COMPANY_ONLY)**
- Bước 16: Chỉ có company rule results, không có legal results
- Scenario = COMPANY_ONLY
- System prompt: "Trả lời dựa trên quy định công ty. Nếu không tìm thấy, nói rõ."
- Response: "Theo Điều 15 Nội quy lao động, nhân viên có 12 ngày phép năm."

**AF2: Query về luật nhà nước (LEGAL_ONLY)**
- Bước 16: Chỉ có legal results, không có company results
- Scenario = LEGAL_ONLY
- System prompt: "Trả lời dựa trên văn bản pháp luật Việt Nam."
- Response: "Theo Điều 24 Bộ luật Lao động 2019, thời gian thử việc tối đa 60 ngày."

**AF3: Query so sánh (COMPARISON)**
- Bước 16: Có cả company results và legal results
- Scenario = COMPARISON
- System prompt: "So sánh quy định công ty với luật nhà nước. Format: [Kết luận], [Quy định công ty], [Luật nhà nước], [Phân tích], [Khuyến nghị]"
- Response:
  ```
  [Kết luận]: Quy định công ty KHÔNG HỢP PHÁP.
  [Quy định công ty]: Thử việc 90 ngày (Điều X Nội quy)
  [Luật nhà nước]: Thử việc tối đa 60 ngày (Điều 24 Bộ luật Lao động 2019)
  [Phân tích]: Quy định công ty vượt quá giới hạn luật quy định.
  [Khuyến nghị]: Điều chỉnh quy định công ty về 60 ngày.
  ```

**AF4: Không tìm thấy context liên quan**
- Bước 15: Vector search không trả về results (similarity < threshold)
- Context = empty
- LLM generate response: "Xin lỗi, tôi không tìm thấy thông tin liên quan trong tài liệu. Bạn có thể hỏi câu khác hoặc liên hệ HR."

### B.5.4. Exception Flows

**EF1: SignalR connection lost**
- Bước 5: SignalR invoke failed
- Frontend auto-reconnect (withAutomaticReconnect)
- Retry send message
- Nếu vẫn fail → Hiển thị error: "Mất kết nối, vui lòng refresh trang"

**EF2: Qdrant unavailable**
- Bước 15: Qdrant API timeout
- ChatProcessor log error
- Return fallback response: "Hệ thống đang bận, vui lòng thử lại sau"
- Publish BotResponseCreatedEvent với fallback response

**EF3: Ollama unavailable**
- Bước 19: Ollama API timeout
- ChatProcessor log error
- Return fallback response: "AI đang bận, vui lòng thử lại sau"

**EF4: RabbitMQ connection lost**
- Bước 9 hoặc 21: RabbitMQ publish failed
- MassTransit auto-retry (5 attempts)
- Nếu vẫn fail → Log critical error
- User không nhận được response → Timeout sau 30 giây → Error message

### B.5.5. Performance Requirements

**PR1:** Embedding time < 500ms
**PR2:** Vector search time < 500ms
**PR3:** LLM generation time < 5-10 giây (depending on response length)
**PR4:** End-to-end latency < 10 giây (p95)

### B.5.6. Quality Requirements

**QR1:** Faithfulness >= 0.85 (RAGAS metric)
**QR2:** Answer Relevancy >= 0.80
**QR3:** Trích dẫn phải chính xác 100% (document_name, heading)

**Sequence Diagram:** `diagrams_to_create.md` → Diagram 4.4 (RAG Pipeline Sequence)

**Activity Diagram:** Tạo activity diagram riêng cho 9-step RAG pipeline

---

## B.6. UC5: Xem lịch sử chat

### B.6.1. Thông tin cơ bản

| Field | Value |
|-------|-------|
| **Use Case ID** | UC5 |
| **Use Case Name** | Xem lịch sử chat |
| **Actors** | Nhân viên |
| **Preconditions** | - User đã đăng nhập<br>- User đã có ít nhất 1 conversation |
| **Postconditions** | - User xem được tất cả conversations của mình<br>- User xem được messages trong conversation |
| **Priority** | Medium |

### B.6.2. Main Flow

**Steps:**
1. User truy cập trang chat
2. Hệ thống load danh sách conversations của user:
   - Query: `ChatConversations.Where(c => c.TenantId == {tenantId} && c.UserId == {userId})`
   - Order by CreatedAt DESC
3. Hệ thống hiển thị sidebar với danh sách conversations:
   - Title
   - Last message preview
   - Timestamp
4. User click vào 1 conversation
5. Hệ thống load messages của conversation:
   - Query: `ChatMessages.Where(m => m.ConversationId == {conversationId})`
   - Order by CreatedAt ASC
6. Hệ thống render messages:
   - User messages (align right, blue background)
   - Bot messages (align left, gray background)
   - Timestamp
7. User có thể scroll để xem old messages
8. User có thể tiếp tục chat trong conversation này (→ UC4)

### B.6.3. Alternative Flows

**AF1: User chưa có conversation nào**
- Bước 2: Query trả về empty list
- Hiển thị: "Bạn chưa có cuộc trò chuyện nào. Nhấn 'New Conversation' để bắt đầu."
- Button "New Conversation"

**AF2: Conversation rất dài (100+ messages)**
- Bước 5: Load chỉ 50 messages gần nhất
- Implement pagination hoặc infinite scroll
- Button "Load more" để load old messages

---

## B.7. UC6: Quản lý tenant (System Admin only)

### B.7.1. Thông tin cơ bản

| Field | Value |
|-------|-------|
| **Use Case ID** | UC6 |
| **Use Case Name** | Quản lý tenant |
| **Actors** | System Admin |
| **Preconditions** | - Admin đã đăng nhập<br>- Admin có quyền system-level |
| **Postconditions** | - Tenant mới được tạo<br>- Admin account cho tenant được tạo |
| **Priority** | Medium |

### B.7.2. Main Flow (Create Tenant)

**Steps:**
1. System Admin truy cập `/api/tenant/create` (hoặc admin UI - future)
2. Hệ thống hiển thị form:
   - Tên công ty (required)
3. Admin nhập tên công ty, click "Create"
4. **TenantService:** Validate tên không rỗng
5. **TenantService:** Tạo Tenant record:
   - Name
   - IsActive = true
6. **TenantService:** (Optional) Tạo admin account đầu tiên cho tenant
7. Return tenant ID
8. Hiển thị thông báo: "Tenant {name} đã được tạo với ID {id}"

### B.7.3. Business Rules

**BR1:** Tenant #1 (Legal Base) là shared tenant, không được xóa
**BR2:** Mỗi tenant cần ít nhất 1 admin account

---

## B.8. UC7: Quản lý Prompt Config

### B.8.1. Thông tin cơ bản

| Field | Value |
|-------|-------|
| **Use Case ID** | UC7 |
| **Use Case Name** | Quản lý Prompt Config |
| **Actors** | HR/Admin |
| **Preconditions** | - User đã đăng nhập<br>- User có role admin |
| **Postconditions** | - PromptConfig được tạo/cập nhật<br>- ChatProcessor sử dụng custom system prompt |
| **Priority** | Low |

### B.8.2. Main Flow (Create/Update Prompt Config)

**Steps:**
1. Admin truy cập trang "Prompt Configuration"
2. Hệ thống hiển thị:
   - Danh sách prompt configs hiện có (của tenant)
   - Button "Create New"
3. Admin click "Create New" hoặc "Edit" một config
4. Hệ thống hiển thị form:
   - Name (required)
   - System Prompt (textarea, required)
5. Admin nhập system prompt custom:
   ```
   Bạn là trợ lý AI chuyên về luật lao động Việt Nam.
   Hãy trả lời ngắn gọn, chính xác, lịch sự.
   Luôn trích dẫn nguồn.
   ```
6. Admin click "Save"
7. **ChatService:** Validate name không rỗng, system prompt không rỗng
8. **ChatService:** Lưu PromptConfig vào database
9. Hiển thị thông báo: "Prompt config đã được lưu"

### B.8.3. Business Rules

**BR1:** Mỗi tenant có thể có nhiều prompt configs
**BR2:** ChatProcessor sử dụng default prompt nếu tenant không có custom config
**BR3:** System prompt phải rõ ràng, tránh hallucination

---

## B.9. Tổng kết

**Nội dung chính:**

### B.9.1. Use Case Coverage

**Core use cases implemented:**
- ✅ UC1: Đăng ký tài khoản
- ✅ UC2: Đăng nhập
- ✅ UC3: Upload tài liệu (với background vectorization)
- ✅ UC4: Hỏi chatbot (RAG pipeline 9 bước)
- ✅ UC5: Xem lịch sử chat
- ✅ UC6: Quản lý tenant
- ✅ UC7: Quản lý prompt config

**Future use cases:**
- ❌ UC8: Đánh giá câu trả lời (thumbs up/down)
- ❌ UC9: Export conversation
- ❌ UC10: Tìm kiếm conversation
- ❌ UC11: Chỉnh sửa/xóa message

### B.9.2. Traceability Matrix

| Use Case | Requirements | Implementation | Test Status |
|----------|--------------|----------------|-------------|
| UC1: Đăng ký | FR1.1 | AccountService/AuthEndpoint.cs | Manual ✅ |
| UC2: Đăng nhập | FR1.2, FR1.3 | AccountService/AuthEndpoint.cs | Manual ✅ |
| UC3: Upload tài liệu | FR3.1-FR3.6 | DocumentService, EmbeddingService | Manual ✅ |
| UC4: Hỏi chatbot | FR4.1-FR4.8 | ChatService, ChatProcessor | Manual ✅ |
| UC5: Xem lịch sử | FR4.1 | ChatService/ChatEndpoint.cs | Manual ✅ |
| UC6: Quản lý tenant | FR2.1-FR2.3 | TenantService/TenantEndpoint.cs | Manual ✅ |
| UC7: Prompt config | FR5.1-FR5.2 | ChatService/PromptConfigEndpoint.cs | Manual ✅ |

**Note:** Tất cả đều manual testing only (no automated tests)

---

**KẾT THÚC PHỤ LỤC B**

**Điểm nhấn chính:**
- ✅ Đặc tả chi tiết 7 use cases chính
- ✅ Main flow, alternative flows, exception flows
- ✅ Preconditions, postconditions, business rules
- ✅ Tham chiếu sequence diagrams, activity diagrams
- ✅ Traceability matrix (use case → requirements → implementation)

**Lưu ý:**
- Activity diagrams cần vẽ riêng cho UC4 (RAG pipeline) - phức tạp nhất
- Có thể tạo thêm use case diagrams chi tiết cho từng actor
