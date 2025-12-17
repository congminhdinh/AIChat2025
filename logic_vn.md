# Kiến Trúc & Quy Trình Xử Lý ChatProcessor

## Tổng Quan

ChatProcessor là dịch vụ AI worker dựa trên Python xử lý tin nhắn chat của người dùng với **Retrieval-Augmented Generation (RAG)** sử dụng bộ lọc tài liệu đa tenant. Nó tích hợp cơ sở dữ liệu vector Qdrant để truy xuất tài liệu và Ollama LLM để tạo phản hồi.

## Điểm Truy Cập

ChatProcessor cung cấp **hai giao diện** chia sẻ cùng logic xử lý cốt lõi:

| Giao Diện | Mô Tả | Vị Trí |
|-----------|-------|--------|
| **RabbitMQ Consumer** | Tích hợp hàng đợi tin nhắn cho môi trường production | `main.py:55-85` |
| **FastAPI REST API** | Endpoint kiểm thử trực tiếp tại `/api/chat/test` | `main.py:87-90` |

Cả hai giao diện đều đảm bảo hành vi nhất quán bằng cách sử dụng cùng hàm `ChatBusiness.process_chat_message()`.

## Kiến Trúc Hệ Thống

```
┌──────────────────┐    UserPromptReceived     ┌───────────────────┐
│   .NET Chat      │ ─────────────────────────>│                   │
│   Service        │                            │  ChatProcessor    │
│                  │ <───────────────────────── │    (Python)       │
└──────────────────┘   BotResponseCreated       └────────┬──────────┘
                                                         │
                    ┌────────────────────────────────────┤
                    │                                    │
              ┌─────▼──────┐                      ┌─────▼──────┐
              │ Embedding  │                      │   Ollama   │
              │  Service   │                      │    LLM     │
              │ (Port 8000)│                      │(Port 11434)│
              └─────┬──────┘                      └────────────┘
                    │
              ┌─────▼──────┐
              │   Qdrant   │
              │   Vector   │
              │     DB     │
              │ (Port 6333)│
              └────────────┘
```

## Quy Trình Xử Lý (Từng Bước)

### Tin Nhắn Đầu Vào
```json
{
  "conversation_id": 1,
  "user_id": 123,
  "message": "Quy định về hợp đồng lao động là gì?",
  "tenant_id": 2
}
```

### Bước 1: Tạo Query Embedding
**Vị trí**: `src/business.py:112`

```python
query_embedding = qdrant_service.get_embedding(message)
```

- **Hành động**: Gọi API EmbeddingService tại `http://localhost:8000/embed`
- **Đầu vào**: Văn bản tin nhắn người dùng
- **Đầu ra**: Vector 768 chiều từ model `truro7/vn-law-embedding`
- **Mục đích**: Chuyển đổi văn bản thành vector cho tìm kiếm ngữ nghĩa

### Bước 2: Truy Xuất Tài Liệu Liên Quan (RAG)
**Vị trí**: `src/business.py:113`

```python
rag_results = await qdrant_service.search_with_tenant_filter(
    query_vector=query_embedding,
    tenant_id=tenant_id,
    limit=settings.rag_top_k
)
```

- **Hành động**: Tìm kiếm trong cơ sở dữ liệu vector Qdrant
- **Collection**: `vn_law_documents`
- **Bộ lọc đa tenant**:
  - `tenant_id = 1` (Tài liệu hệ thống/chia sẻ) **HOẶC**
  - `tenant_id = input_tenant_id` (Tài liệu riêng của tenant)
- **Giới hạn**: Top K tài liệu (mặc định: 5)
- **Mục đích**: Tìm tài liệu ngữ cảnh liên quan cho câu hỏi người dùng

### Bước 3: Trích Xuất Ngữ Cảnh & Source IDs
**Vị trí**: `src/business.py:115-121`

```python
context_texts = []
source_ids = []
for result in rag_results:
    if hasattr(result, 'payload') and 'text' in result.payload:
        context_texts.append(result.payload['text'])
        if 'source_id' in result.payload:
            source_ids.append(result.payload['source_id'])
```

- **Hành động**: Trích xuất nội dung văn bản và ID theo dõi nguồn
- **Mục đích**: Chuẩn bị ngữ cảnh cho prompt và theo dõi nguồn tài liệu

### Bước 4: Xây Dựng Prompt Nâng Cao
**Vị trí**: `src/business.py:123-132`

**Nếu tìm thấy tài liệu**:
```
Context information:
[Văn bản tài liệu 1]

[Văn bản tài liệu 2]

...

User question: Quy định về hợp đồng lao động là gì?

Please answer based on the context provided above.
```

**Nếu không tìm thấy tài liệu**:
```
Quy định về hợp đồng lao động là gì?
```

- **Mục đích**: Nâng cao câu hỏi gốc với ngữ cảnh liên quan từ RAG

### Bước 5: Tạo Phản Hồi AI
**Vị trí**: `src/business.py:134`

```python
ai_response = await ollama_service.generate_response(
    prompt=enhanced_prompt,
    conversation_history=None
)
```

- **Hành động**: Gọi API Ollama tại `http://localhost:11434/api/chat`
- **Model**: `ontocord/vistral:latest` (Vietnamese 7B LLM, lượng tử hóa Q4_0)
- **Timeout**: 300 giây
- **Chế độ**: Stateless (không duy trì lịch sử cuộc trò chuyện)
- **Mục đích**: Tạo phản hồi ngôn ngữ tự nhiên dựa trên ngữ cảnh

### Phản Hồi Đầu Ra
**Vị trí**: `src/business.py:137-144`

```json
{
  "conversation_id": 1,
  "message": "Theo luật lao động Việt Nam, hợp đồng lao động phải bao gồm...",
  "user_id": 0,
  "timestamp": "2025-12-17T10:30:45.123456",
  "model_used": "ontocord/vistral:latest",
  "rag_documents_used": 3,
  "source_ids": ["doc-123", "doc-456", "doc-789"]
}
```

---

# DocumentService - Pipeline Embedding Tài Liệu

## Tổng Quan

DocumentService là dịch vụ dựa trên .NET quản lý việc tải lên, xử lý và vector hóa tài liệu. Nó sử dụng **Hangfire** để xử lý background job, xử lý việc chia nhỏ tài liệu và tạo embedding một cách bất đồng bộ.

## Kiến Trúc Xử Lý Tài Liệu

```
┌──────────────┐     Upload File      ┌────────────────────┐
│   User/API   │ ──────────────────> │  DocumentService   │
└──────────────┘                      │     (.NET)         │
                                      └──────┬─────────────┘
                                             │
                    ┌────────────────────────┼────────────────────┐
                    │                        │                    │
              ┌─────▼──────┐          ┌─────▼──────┐      ┌─────▼──────┐
              │  Storage   │          │  Hangfire  │      │ Embedding  │
              │  Service   │          │   Queue    │      │  Service   │
              └────────────┘          └─────┬──────┘      └────────────┘
                                            │                     │
                                      ┌─────▼──────┐              │
                                      │Background  │              │
                                      │   Jobs     │──────────────┘
                                      └─────┬──────┘
                                            │
                                      ┌─────▼──────┐
                                      │   Qdrant   │
                                      │  Database  │
                                      └────────────┘
```

## Quy Trình Vector Hóa Tài Liệu

### Điểm Truy Cập: Vector Hóa Tài Liệu
**Endpoint**: `POST /web-api/document/vectorize/{documentId}`
**Vị trí**: `DocumentEndpoint.cs:67-73`

### Bước 1: Cập Nhật Trạng Thái Tài Liệu
**Vị trí**: `PromptDocumentBusiness.cs:236-237`

```csharp
document.Action = DocumentAction.Vectorize_Start;
await _documentRepository.UpdateAsync(document);
```

- **Hành động**: Đánh dấu tài liệu đang bắt đầu vector hóa
- **Mục đích**: Theo dõi trạng thái xử lý trong cơ sở dữ liệu

### Bước 2: Tải Tài Liệu Từ Storage
**Vị trí**: `PromptDocumentBusiness.cs:241-249`

```csharp
var downloadUrl = $"{_appSettings.ApiGatewayUrl}/web-api/storage/download-file?filePath={document.FilePath}";
var fileStream = await GetStreamAsync(downloadUrl);
```

- **Hành động**: Tải file .docx từ StorageService
- **Đầu vào**: Đường dẫn file từ cơ sở dữ liệu
- **Đầu ra**: Stream file để xử lý

### Bước 3: Trích Xuất Các Chunk Theo Cấu Trúc Phân Cấp
**Vị trí**: `PromptDocumentBusiness.cs:251`, `ExtractHierarchicalChunks:395-460`

```csharp
var chunks = ExtractHierarchicalChunks(memoryStream, documentId, document.FileName);
```

#### Cấu Trúc Phân Cấp

Tài liệu pháp luật Việt Nam tuân theo cấu trúc này:
- **Heading1 (Chương)**: Chương - ví dụ: "CHƯƠNG I: QUY ĐỊNH CHUNG"
- **Heading2 (Mục)**: Mục - ví dụ: "Mục 1: Phạm vi điều chỉnh"
- **Heading3 (Điều)**: Điều khoản - ví dụ: "Điều 1. Định nghĩa"
- **Content**: Các đoạn văn bản thuộc mỗi điều

#### Logic Trích Xuất

**Vị trí**: `PromptDocumentBusiness.cs:409-453`

```csharp
foreach (var para in body.Elements<Paragraph>())
{
    var text = para.InnerText.Trim();

    if (_regexHeading1.IsMatch(text))  // Chương
    {
        FlushChunk(...);  // Lưu chunk trước đó
        currentHeading1 = text;
        currentHeading2 = null;
        currentHeading3 = string.Empty;
    }
    else if (_regexHeading2.IsMatch(text))  // Mục
    {
        FlushChunk(...);
        currentHeading2 = text;
        currentHeading3 = string.Empty;
    }
    else if (_regexHeading3.IsMatch(text))  // Điều
    {
        FlushChunk(...);
        currentHeading3 = text;
        contentParagraphs.Clear();
    }
    else  // Nội dung chính
    {
        if (!string.IsNullOrEmpty(currentHeading3))
            contentParagraphs.Add(text);
    }
}
```

#### Cấu Trúc Chunk

Mỗi chunk chứa:

```csharp
{
    Heading1: "CHƯƠNG I: QUY ĐỊNH CHUNG",
    Heading2: "Mục 1: Phạm vi điều chỉnh",
    Content: "Điều 1. Định nghĩa\n[Các đoạn văn bản...]",
    FullText: "[Heading1]\n[Heading2]\n[Content]",  // Dùng cho embedding
    DocumentId: 123,
    FileName: "labor-law.docx"
}
```

- **FullText**: Văn bản kết hợp của tất cả các cấp độ phân cấp để có ngữ cảnh phong phú
- **Mục đích**: Bảo toàn cấu trúc tài liệu để tìm kiếm chính xác hơn

### Bước 4: Tạo Các Batch
**Vị trí**: `PromptDocumentBusiness.cs:261-266`

```csharp
const int batchSize = 10;
var batches = new List<List<DocumentChunkDto>>();
for (int i = 0; i < chunks.Count; i += batchSize)
{
    batches.Add(chunks.Skip(i).Take(batchSize).ToList());
}
```

- **Kích thước batch**: 10 chunks mỗi batch
- **Mục đích**: Xử lý nhiều chunks song song để tăng hiệu suất

### Bước 5: Đưa Vào Hàng Đợi Hangfire Background Jobs
**Vị trí**: `PromptDocumentBusiness.cs:268-272`

```csharp
foreach (var batch in batches)
{
    _backgroundJobClient.Enqueue<VectorizeBackgroundJob>(
        job => job.ProcessBatch(batch, tenantId));
}
```

- **Hành động**: Tạo background jobs cho mỗi batch
- **Loại Job**: `VectorizeBackgroundJob.ProcessBatch`
- **Thực thi**: Bất đồng bộ, được xử lý bởi Hangfire workers
- **Mục đích**: Giảm tải xử lý nặng khỏi luồng chính

### Bước 6: Cập Nhật Trạng Thái Tài Liệu (Thành công)
**Vị trí**: `PromptDocumentBusiness.cs:274-275`

```csharp
document.Action = DocumentAction.Vectorize_Success;
await _documentRepository.UpdateAsync(document);
```

## Xử Lý Background Job

### VectorizeBackgroundJob.ProcessBatch
**Vị trí**: `VectorizeBackgroundJob.cs:25-65`

Job này chạy bất đồng bộ trong các worker thread của Hangfire.

#### Bước 1: Xây Dựng Batch Request
**Vị trí**: `VectorizeBackgroundJob.cs:29-45`

```csharp
var batchRequest = new BatchVectorizeRequestDto
{
    Items = chunks.Select(chunk => new VectorizeRequestDto
    {
        Text = chunk.FullText,  // Văn bản phân cấp đầy đủ
        Metadata = new Dictionary<string, object>
        {
            { "source_id", chunk.DocumentId },
            { "file_name", chunk.FileName },
            { "heading1", chunk.Heading1 },
            { "heading2", chunk.Heading2 },
            { "content", chunk.Content },
            { "tenant_id", tenantId },
            { "type", 1 }  // Loại tài liệu
        }
    }).ToList()
};
```

- **Text**: FullText với toàn bộ ngữ cảnh phân cấp
- **Metadata**: Các trường có thể tìm kiếm/lọc được lưu cùng vector
- **tenant_id**: Cho phép lọc đa tenant
- **source_id**: Liên kết vector trở lại tài liệu gốc

#### Bước 2: Gọi API EmbeddingService
**Vị trí**: `VectorizeBackgroundJob.cs:47-48`

```csharp
var vectorizeUrl = $"{_appSettings.EmbeddingServiceUrl}/vectorize-batch";
var response = await PostAsync<BatchVectorizeRequestDto, VectorizeResponseDto>(vectorizeUrl, batchRequest);
```

- **Endpoint**: `POST /vectorize-batch`
- **Hành động**: Tạo embeddings và lưu vào Qdrant
- **Model**: `truro7/vn-law-embedding` (768 chiều)
- **Collection**: `vn_law_documents`

#### Xử Lý EmbeddingService

**Vị trí**: `EmbeddingService/src/business.py:79-105`

Cho mỗi item trong batch:
1. **Tạo Embedding**: Chuyển đổi văn bản thành vector 768 chiều sử dụng `truro7/vn-law-embedding`
2. **Tạo Point**: Gói vector với metadata và ID duy nhất
3. **Upsert vào Qdrant**: Lưu vector vào collection `vn_law_documents`

```python
for item in items:
    embedding = self.encode_text(item.text)  # Vector 768 chiều
    point_id = str(uuid.uuid4())

    points.append(PointStruct(
        id=point_id,
        vector=embedding,
        payload={"text": item.text, **item.metadata}
    ))

self.qdrant_client.upsert(collection_name=collection_name, points=points)
```

#### Bước 3: Xử Lý Response
**Vị trí**: `VectorizeBackgroundJob.cs:50-58`

```csharp
if (response?.success == true)
{
    _logger.LogInformation("Successfully vectorized batch of {ChunkCount} chunks", chunks.Count);
}
else
{
    _logger.LogError("Failed to vectorize batch of {ChunkCount} chunks", chunks.Count);
    throw new Exception($"Vectorization failed for batch of {chunks.Count} chunks");
}
```

## Các Trạng Thái Tài Liệu

| Trạng Thái | Mô Tả | Vị Trí |
|-----------|--------|---------|
| `Upload` | Tài liệu đã tải lên StorageService | Sau khi tải file |
| `Standardization` | Đã áp dụng kiểu heading cho .docx | Trong quá trình tải lên |
| `Vectorize_Start` | Quá trình vector hóa đã bắt đầu | Trước khi chia nhỏ |
| `Vectorize_Success` | Tất cả chunks đã được embed thành công | Sau khi tất cả jobs hoàn thành |
| `Vectorize_Failed` | Vector hóa thất bại | Khi có lỗi |

## Quy Trình Xóa Tài Liệu

**Endpoint**: `DELETE /web-api/document/{id}`
**Vị trí**: `PromptDocumentBusiness.cs:177-225`

### Xóa Hai Giai Đoạn

#### Giai Đoạn 1: Xóa Khỏi Cơ Sở Dữ Liệu SQL
```csharp
await _documentRepository.DeleteAsync(document);  // Soft delete
```

#### Giai Đoạn 2: Xóa Vectors Khỏi Qdrant
**Vị trí**: `PromptDocumentBusiness.cs:193-216`

```csharp
var deleteUrl = $"{_appSettings.EmbeddingServiceUrl}/api/embeddings/delete";
var deleteRequest = new
{
    source_id = input.DocumentId,
    tenant_id = tenantId,
    type = 1,
    collection_name = "vn_law_documents"
};

await PostAsync<object, object>(deleteUrl, deleteRequest);
```

**Xóa EmbeddingService**: `EmbeddingService/src/business.py:107-122`

```python
delete_filter = Filter(
    must=[
        FieldCondition(key="source_id", match=MatchValue(value=source_id)),
        FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id)),
        FieldCondition(key="type", match=MatchValue(value=type))
    ]
)

self.qdrant_client.delete(
    collection_name=collection_name,
    points_selector=FilterSelector(filter=delete_filter)
)
```

- **Bộ lọc**: Khớp tất cả vectors với source_id, tenant_id và type
- **Kết quả**: Xóa tất cả chunks từ tài liệu
- **Tính nhất quán**: Nhất quán mềm - ghi log lỗi nhưng không rollback DB nếu xóa vector thất bại

## Cấu Hình

### appsettings.json

```json
{
  "ApiGatewayUrl": "http://localhost:5000",
  "EmbeddingServiceUrl": "http://localhost:8000",
  "RegexHeading1": "^CHƯƠNG\\s+[IVXLCDM]+",
  "RegexHeading2": "^Mục\\s+\\d+",
  "RegexHeading3": "^Điều\\s+\\d+"
}
```

### Mẫu Regex

| Mẫu | Khớp với | Ví dụ |
|-----|----------|-------|
| `RegexHeading1` | Chương với số La Mã | "CHƯƠNG I: QUY ĐỊNH CHUNG" |
| `RegexHeading2` | Mục với số thập phân | "Mục 1: Phạm vi điều chỉnh" |
| `RegexHeading3` | Điều với số thập phân | "Điều 1. Định nghĩa" |

## Cân Nhắc Về Hiệu Suất

- **Chia nhỏ**: ~100-500ms mỗi tài liệu (tùy thuộc kích thước)
- **Kích thước batch**: 10 chunks mỗi job (có thể cấu hình)
- **Tạo embedding**: ~100-300ms mỗi chunk
- **Tổng thời gian**: Bất đồng bộ, không chặn request người dùng
- **Hangfire**: Xử lý jobs trong các worker thread nền

## Ví Dụ: Xử Lý Tài Liệu Hoàn Chỉnh

### Cấu Trúc Tài Liệu Đầu Vào
```
CHƯƠNG I: QUY ĐỊNH CHUNG
Mục 1: Phạm vi điều chỉnh
Điều 1. Định nghĩa
Hợp đồng lao động là...

Điều 2. Phạm vi
Bộ luật này áp dụng...

Mục 2: Quyền và nghĩa vụ
Điều 3. Quyền của người lao động
Người lao động có quyền...
```

### Các Chunks Được Trích Xuất (3 chunks)

**Chunk 1**:
```
FullText: "CHƯƠNG I: QUY ĐỊNH CHUNG\nMục 1: Phạm vi điều chỉnh\nĐiều 1. Định nghĩa\nHợp đồng lao động là..."
Metadata: {
  heading1: "CHƯƠNG I: QUY ĐỊNH CHUNG",
  heading2: "Mục 1: Phạm vi điều chỉnh",
  content: "Điều 1. Định nghĩa\nHợp đồng lao động là...",
  source_id: 123,
  tenant_id: 2
}
```

**Chunk 2**:
```
FullText: "CHƯƠNG I: QUY ĐỊNH CHUNG\nMục 1: Phạm vi điều chỉnh\nĐiều 2. Phạm vi\nBộ luật này áp dụng..."
```

**Chunk 3**:
```
FullText: "CHƯƠNG I: QUY ĐỊNH CHUNG\nMục 2: Quyền và nghĩa vụ\nĐiều 3. Quyền của người lao động\nNgười lao động có quyền..."
```

### Quy Trình Xử Lý

1. Người dùng tải lên tài liệu → DocumentId: 123
2. Trạng thái tài liệu: `Upload` → `Vectorize_Start`
3. Trích xuất 3 chunks với cấu trúc phân cấp
4. Tạo 1 batch (3 chunks < 10)
5. Đưa vào hàng đợi 1 Hangfire job
6. Trạng thái tài liệu: `Vectorize_Success`
7. Background job gọi EmbeddingService
8. 3 vectors được lưu vào Qdrant cùng metadata
9. Vectors sẵn sàng cho các truy vấn RAG của ChatProcessor

---

## Các Phụ Thuộc Bên Ngoài

### 1. EmbeddingService (Port 8000)
- **Endpoint**: `POST /embed`
- **Model**: `truro7/vn-law-embedding`
- **Đầu ra**: Embeddings 768 chiều
- **Mục đích**: Chuyển đổi văn bản thành vectors cho tìm kiếm ngữ nghĩa

### 2. Qdrant Vector Database (Port 6333)
- **Collection**: `vn_law_documents`
- **Số chiều vector**: 768
- **Độ đo khoảng cách**: Cosine similarity
- **Mục đích**: Lưu trữ và truy xuất document vectors với bộ lọc đa tenant

### 3. Ollama LLM Engine (Port 11434)
- **Model**: `ontocord/vistral:latest`
- **Kích thước**: 7B parameters (lượng tử hóa Q4_0)
- **Ngôn ngữ**: Tối ưu cho tiếng Việt
- **Mục đích**: Tạo phản hồi ngôn ngữ tự nhiên

### 4. RabbitMQ Message Broker (Port 5672) [Tùy chọn]
- **Hàng đợi đầu vào**: `UserPromptReceived`
- **Hàng đợi đầu ra**: `BotResponseCreated`
- **Mục đích**: Xử lý tin nhắn bất đồng bộ cho production

## Tính Năng Chính

### Hỗ Trợ Đa Tenant
**Triển khai**: `src/business.py:73-78`

```python
search_filter = Filter(
    should=[
        FieldCondition(key="tenant_id", match=MatchValue(value=1)),
        FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id))
    ]
)
```

- **Tenant 1**: Tài liệu hệ thống/chia sẻ có thể truy cập bởi tất cả người dùng
- **Tenants khác**: Tài liệu riêng tư của từng tenant
- **Logic**: Mỗi truy vấn tìm kiếm cả tài liệu chia sẻ và tài liệu riêng của tenant

### Xử Lý Stateless
- Mỗi tin nhắn được xử lý độc lập
- Không duy trì lịch sử cuộc trò chuyện
- Không có ngữ cảnh từ tin nhắn trước
- Đơn giản hóa việc mở rộng và giảm sử dụng bộ nhớ

### Nâng Cao RAG Có Điều Kiện
- **Tìm thấy tài liệu**: Thêm ngữ cảnh vào prompt để có phản hồi chính xác
- **Không tìm thấy tài liệu**: Chuyển tin nhắn gốc trực tiếp đến LLM
- Fallback graceful đảm bảo hệ thống luôn phản hồi

### Xử Lý Lỗi
- Logging toàn diện ở mỗi bước
- Theo dõi Conversation ID để debug
- Kiểm tra sức khỏe cho tất cả dịch vụ bên ngoài
- Degradation graceful khi dịch vụ gặp sự cố

## Cấu Hình

### Biến Môi Trường (.env)

```bash
# Cấu hình RabbitMQ
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_QUEUE_INPUT=UserPromptReceived
RABBITMQ_QUEUE_OUTPUT=BotResponseCreated

# Cấu hình Ollama
OLLAMA_BASE_URL=http://localhost:11434
OLLAMA_MODEL=ontocord/vistral:latest
OLLAMA_TIMEOUT=300

# Cấu hình Qdrant
QDRANT_HOST=localhost
QDRANT_PORT=6333
QDRANT_COLLECTION=vn_law_documents
RAG_TOP_K=5

# Cấu hình Embedding Service
EMBEDDING_SERVICE_URL=http://localhost:8000

# Cấu hình FastAPI
FASTAPI_HOST=0.0.0.0
FASTAPI_PORT=8001

# Cấu hình Service
LOG_LEVEL=INFO
PREFETCH_COUNT=1
```

### Các Tham Số Cấu Hình Chính

| Tham Số | Mặc Định | Mô Tả |
|---------|----------|-------|
| `RAG_TOP_K` | 5 | Số lượng tài liệu truy xuất từ Qdrant |
| `OLLAMA_TIMEOUT` | 300 | Thời gian tối đa (giây) để tạo phản hồi LLM |
| `EMBEDDING_SERVICE_URL` | http://localhost:8000 | Endpoint API EmbeddingService |
| `FASTAPI_PORT` | 8001 | Port API REST để kiểm thử trực tiếp |
| `QDRANT_COLLECTION` | vn_law_documents | Tên collection cơ sở dữ liệu vector |

## Cấu Trúc Dự Án

```
ChatProcessor/
├── main.py                    # Điểm vào (RabbitMQ + FastAPI)
├── src/
│   ├── config.py             # Cài đặt cấu hình
│   ├── business.py           # Logic xử lý cốt lõi
│   │   ├── OllamaService     # Tích hợp Ollama LLM
│   │   ├── QdrantService     # Tích hợp Qdrant + Embedding
│   │   └── ChatBusiness      # Điều phối xử lý chính
│   ├── consumer.py           # RabbitMQ consumer
│   ├── router.py             # Routes FastAPI
│   └── schemas.py            # Schemas tin nhắn
├── app/                      # Cấu trúc app thay thế
│   ├── main.py
│   ├── api.py
│   ├── config.py
│   └── services/
│       ├── service.py
│       ├── ollama_service.py
│       ├── qdrant_service.py
│       └── rabbitmq_service.py
├── requirements.txt
├── .env
└── README.md
```

## Chạy Dịch Vụ

### Yêu Cầu Tiên Quyết
1. Python 3.11+
2. EmbeddingService đang chạy trên port 8000
3. Qdrant đang chạy trên port 6333 với collection `vn_law_documents`
4. Ollama đang chạy trên port 11434 với model `ontocord/vistral:latest`
5. RabbitMQ đang chạy trên port 5672 (tùy chọn cho kiểm thử)

### Khởi Động Dịch Vụ

```bash
cd Services/ChatProcessor
python main.py
```

Logs dịch vụ sẽ hiển thị:
```
Starting ChatProcessor Service
Ollama URL: http://localhost:11434
Ollama Model: ontocord/vistral:latest
RabbitMQ Host: localhost:5672
Qdrant Host: localhost:6333
Performing health checks...
ChatProcessor running with RabbitMQ consumer and FastAPI endpoint
FastAPI available at http://0.0.0.0:8001
```

### Kiểm Thử Với REST API

```bash
curl -X POST "http://localhost:8001/api/chat/test" \
  -H "Content-Type: application/json" \
  -d '{
    "conversation_id": 1,
    "message": "Quy định pháp luật lao động Việt Nam là gì?",
    "user_id": 123,
    "tenant_id": 2
  }'
```

### Kiểm Tra Sức Khỏe

```bash
curl http://localhost:8001/health
```

Phản hồi:
```json
{
  "status": "healthy",
  "ollama": true,
  "qdrant": true
}
```

## Cân Nhắc Về Hiệu Suất

- **Tạo Embedding**: ~100-300ms (phụ thuộc độ dài văn bản)
- **Tìm kiếm Vector**: ~10-50ms (phụ thuộc kích thước collection)
- **Tạo LLM**: 5-60 giây (phụ thuộc độ dài phản hồi và tốc độ model)
- **Tổng Thời Gian Xử Lý**: Thường 5-60 giây mỗi tin nhắn

## Khắc Phục Sự Cố

### Lỗi Vector Dimension Mismatch
**Lỗi**: `expected dim: 768, got 384`
- Đảm bảo EmbeddingService đang chạy và có thể truy cập
- Xác minh `EMBEDDING_SERVICE_URL` trỏ đến đúng dịch vụ

### Lỗi Ollama 404
**Lỗi**: `Ollama error: 404`
- Kiểm tra tên model khớp với các models có sẵn: `curl http://localhost:11434/api/tags`
- Pull model nếu thiếu: `ollama pull ontocord/vistral:latest`

### Không Tìm Thấy Collection
**Lỗi**: `Collection 'vn_law_documents' doesn't exist`
- Tạo collection qua DocumentService hoặc EmbeddingService
- Cập nhật `QDRANT_COLLECTION` trong .env để khớp với collection hiện có

### Connection Refused
- Xác minh tất cả dịch vụ đang chạy:
  - `curl http://localhost:8000/health` (EmbeddingService)
  - `curl http://localhost:6333/collections` (Qdrant)
  - `curl http://localhost:11434/api/tags` (Ollama)
  - `rabbitmqctl status` (RabbitMQ)

## Cải Tiến Trong Tương Lai

1. **Lịch Sử Cuộc Trò Chuyện**: Duy trì ngữ cảnh qua nhiều tin nhắn
2. **Caching**: Cache embeddings và phản hồi LLM cho các truy vấn phổ biến
3. **Streaming Responses**: Stream đầu ra LLM để cải thiện UX
4. **Hybrid Search**: Kết hợp tìm kiếm vector với tìm kiếm từ khóa
5. **Đánh Giá Chất Lượng Phản Hồi**: Theo dõi và cải thiện độ chính xác phản hồi
6. **A/B Testing**: So sánh các chiến lược prompting khác nhau

---

**Cập Nhật Lần Cuối**: 2025-12-17
**Phiên Bản**: 1.0.0
