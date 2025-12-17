# ChatProcessor Architecture & Processing Flow

## Overview

ChatProcessor is a Python-based AI worker service that processes user chat messages with **Retrieval-Augmented Generation (RAG)** using multi-tenant document filtering. It integrates Qdrant vector database for document retrieval and Ollama LLM for response generation.

## Entry Points

The ChatProcessor provides **two interfaces** that share the same core processing logic:

| Interface | Description | Location |
|-----------|-------------|----------|
| **RabbitMQ Consumer** | Production message queue integration | `main.py:55-85` |
| **FastAPI REST API** | Direct testing endpoint at `/api/chat/test` | `main.py:87-90` |

Both interfaces ensure consistent behavior by using the same `ChatBusiness.process_chat_message()` function.

## System Architecture

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

## Processing Flow (Step-by-Step)

### Input Message
```json
{
  "conversation_id": 1,
  "user_id": 123,
  "message": "What are the regulations about labor contracts?",
  "tenant_id": 2
}
```

### Step 1: Generate Query Embedding
**Location**: `src/business.py:112`

```python
query_embedding = qdrant_service.get_embedding(message)
```

- **Action**: Calls EmbeddingService API at `http://localhost:8000/embed`
- **Input**: User message text
- **Output**: 768-dimensional vector from `truro7/vn-law-embedding` model
- **Purpose**: Convert text to vector for semantic search

### Step 2: Retrieve Relevant Documents (RAG) - Dual Query Strategy
**Location**: `src/business.py:154-175`

#### 2.1: Execute Parallel Queries

```python
# Query A: Legal Base (tenant_id = 1)
legal_base_task = qdrant_service.search_exact_tenant(
    query_vector=query_embedding,
    tenant_id=1,
    limit=1
)

# Query B: Company Rule (tenant_id = current_user_tenant)
company_rule_task = qdrant_service.search_exact_tenant(
    query_vector=query_embedding,
    tenant_id=tenant_id,
    limit=1
)

# Execute both queries in parallel
legal_base_results, company_rule_results = await asyncio.gather(
    legal_base_task,
    company_rule_task,
    return_exceptions=True
)
```

- **Action**: Executes TWO separate queries in parallel using `asyncio.gather`
- **Collection**: `vn_law_documents`
- **Query A (Legal Base)**:
  - `tenant_id = 1` (State law documents)
  - Limit: 1 document
  - Filter: Exact match using `must` condition
- **Query B (Company Rule)**:
  - `tenant_id = current_user_tenant` (Company-specific documents)
  - Limit: 1 document
  - Filter: Exact match using `must` condition
- **Purpose**: Guarantee retrieval from BOTH sources for strict cross-referencing

#### Why Dual Queries?

**Problem with Previous Approach** (`search_with_tenant_filter` using OR logic):
```python
# Old approach - PROBLEMATIC
search_filter = Filter(
    should=[  # OR condition
        FieldCondition(key="tenant_id", match=MatchValue(value=1)),
        FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id))
    ]
)
```
- Could return 2 Law chunks OR 2 Company chunks based on similarity scores
- No guarantee of cross-referencing both sources
- LLM might miss comparison opportunities

**Solution with Dual Queries**:
- Separate queries with `must` (exact match) filters
- Always retrieves from BOTH sources when available
- Enables true comparison and compliance checking
- Vector similarity cannot bias results toward one source

### Step 3: Build Labeled Context from Dual Results
**Location**: `src/business.py:177-212`

#### 3.1: Handle Query Errors
```python
# Handle potential errors from parallel execution
if isinstance(legal_base_results, Exception):
    logger.error(f"Legal base query failed: {legal_base_results}")
    legal_base_results = []
if isinstance(company_rule_results, Exception):
    logger.error(f"Company rule query failed: {company_rule_results}")
    company_rule_results = []
```

#### 3.2: Process Results with Labels
```python
context_sections = []
source_ids = []
documents_used = 0

# Process legal base results
if legal_base_results and len(legal_base_results) > 0:
    for result in legal_base_results:
        if hasattr(result, 'payload') and 'text' in result.payload:
            context_sections.append(f"[STATE LAW]\n{result.payload['text']}")
            if 'source_id' in result.payload:
                source_ids.append(result.payload['source_id'])
            documents_used += 1

# Process company rule results
if company_rule_results and len(company_rule_results) > 0:
    for result in company_rule_results:
        if hasattr(result, 'payload') and 'text' in result.payload:
            context_sections.append(f"[COMPANY REGULATION]\n{result.payload['text']}")
            if 'source_id' in result.payload:
                source_ids.append(result.payload['source_id'])
            documents_used += 1
```

- **Action**: Extract text content with source-specific labels
- **Labels**:
  - `[STATE LAW]`: Documents from tenant_id=1 (legal base)
  - `[COMPANY REGULATION]`: Documents from user's tenant_id
- **Error Handling**: Logs warnings when documents are missing from either source
- **Purpose**: Provide clear attribution and enable LLM to compare sources

### Step 4: Build Enhanced Prompt with Cross-Reference Instructions
**Location**: `src/business.py:214-225`

**If documents found from both sources**:
```
Context information:
[STATE LAW]
Điều 10. Hợp đồng lao động
Hợp đồng lao động phải được ký kết bằng văn bản...

[COMPANY REGULATION]
Quy định nội bộ về hợp đồng
Công ty yêu cầu tất cả hợp đồng phải có xác nhận từ phòng nhân sự...

User question: What are the regulations about labor contracts?

Please answer based on the context provided above. If both STATE LAW and COMPANY REGULATION are provided, compare and contrast them in your response.
```

**If only one source found**:
```
Context information:
[STATE LAW]
Điều 10. Hợp đồng lao động...

User question: What are the regulations about labor contracts?

Please answer based on the context provided above. If both STATE LAW and COMPANY REGULATION are provided, compare and contrast them in your response.
```

**If no documents found**:
```
What are the regulations about labor contracts?
```

- **Purpose**:
  - Enhance question with labeled context from both sources
  - Instruct LLM to compare and contrast when both sources available
  - Enable compliance checking (company rules vs state law)
- **Labels**: Clear attribution helps LLM and users identify source of each statement

### Step 5: Generate AI Response
**Location**: `src/business.py:227`

```python
ai_response = await ollama_service.generate_response(
    prompt=enhanced_prompt,
    conversation_history=None
)
```

- **Action**: Calls Ollama API at `http://localhost:11434/api/chat`
- **Model**: `ontocord/vistral:latest` (Vietnamese 7B LLM, Q4_0 quantization)
- **Timeout**: 300 seconds
- **Mode**: Stateless (no conversation history)
- **Purpose**: Generate natural language response based on dual-source context

### Output Response
**Location**: `src/business.py:230-237`

```json
{
  "conversation_id": 1,
  "message": "According to Vietnamese labor law, labor contracts must include...",
  "user_id": 0,
  "timestamp": "2025-12-17T10:30:45.123456",
  "model_used": "ontocord/vistral:latest",
  "rag_documents_used": 3,
  "source_ids": ["doc-123", "doc-456", "doc-789"]
}
```

---

# DocumentService - Document Embedding Pipeline

## Overview

DocumentService is a .NET-based service that manages document upload, processing, and vectorization. It uses **Hangfire** for background job processing to handle document chunking and embedding generation asynchronously.

## Document Processing Architecture

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

## Document Vectorization Flow

### Entry Point: Vectorize Document
**Endpoint**: `POST /web-api/document/vectorize/{documentId}`
**Location**: `DocumentEndpoint.cs:67-73`

### Step 1: Update Document Status
**Location**: `PromptDocumentBusiness.cs:236-237`

```csharp
document.Action = DocumentAction.Vectorize_Start;
await _documentRepository.UpdateAsync(document);
```

- **Action**: Mark document as starting vectorization
- **Purpose**: Track processing status in database

### Step 2: Download Document from Storage
**Location**: `PromptDocumentBusiness.cs:241-249`

```csharp
var downloadUrl = $"{_appSettings.ApiGatewayUrl}/web-api/storage/download-file?filePath={document.FilePath}";
var fileStream = await GetStreamAsync(downloadUrl);
```

- **Action**: Download .docx file from StorageService
- **Input**: File path from database
- **Output**: File stream for processing

### Step 3: Extract Hierarchical Chunks
**Location**: `PromptDocumentBusiness.cs:251`, `ExtractHierarchicalChunks:395-460`

```csharp
var chunks = ExtractHierarchicalChunks(memoryStream, documentId, document.FileName);
```

#### Hierarchical Structure

Vietnamese legal documents follow this structure:
- **Heading1 (Chương)**: Chapter - e.g., "CHƯƠNG I: QUY ĐỊNH CHUNG"
- **Heading2 (Mục)**: Section - e.g., "Mục 1: Phạm vi điều chỉnh"
- **Heading3 (Điều)**: Article - e.g., "Điều 1. Định nghĩa"
- **Content**: Body paragraphs under each article

#### Extraction Logic

**Location**: `PromptDocumentBusiness.cs:409-453`

```csharp
foreach (var para in body.Elements<Paragraph>())
{
    var text = para.InnerText.Trim();

    if (_regexHeading1.IsMatch(text))  // Chapter
    {
        FlushChunk(...);  // Save previous chunk
        currentHeading1 = text;
        currentHeading2 = null;
        currentHeading3 = string.Empty;
    }
    else if (_regexHeading2.IsMatch(text))  // Section
    {
        FlushChunk(...);
        currentHeading2 = text;
        currentHeading3 = string.Empty;
    }
    else if (_regexHeading3.IsMatch(text))  // Article
    {
        FlushChunk(...);
        currentHeading3 = text;
        contentParagraphs.Clear();
    }
    else  // Body content
    {
        if (!string.IsNullOrEmpty(currentHeading3))
            contentParagraphs.Add(text);
    }
}
```

#### Chunk Structure

Each chunk contains:

```csharp
{
    Heading1: "CHƯƠNG I: QUY ĐỊNH CHUNG",
    Heading2: "Mục 1: Phạm vi điều chỉnh",
    Content: "Điều 1. Định nghĩa\n[Body paragraphs...]",
    FullText: "[Heading1]\n[Heading2]\n[Content]",  // Used for embedding
    DocumentId: 123,
    FileName: "labor-law.docx"
}
```

- **FullText**: Combined text of all hierarchical levels for rich context
- **Purpose**: Preserve document structure for better search relevance

### Step 4: Create Batches
**Location**: `PromptDocumentBusiness.cs:261-266`

```csharp
const int batchSize = 10;
var batches = new List<List<DocumentChunkDto>>();
for (int i = 0; i < chunks.Count; i += batchSize)
{
    batches.Add(chunks.Skip(i).Take(batchSize).ToList());
}
```

- **Batch Size**: 10 chunks per batch
- **Purpose**: Process multiple chunks in parallel for efficiency

### Step 5: Enqueue Hangfire Background Jobs
**Location**: `PromptDocumentBusiness.cs:268-272`

```csharp
foreach (var batch in batches)
{
    _backgroundJobClient.Enqueue<VectorizeBackgroundJob>(
        job => job.ProcessBatch(batch, tenantId));
}
```

- **Action**: Create background jobs for each batch
- **Job Type**: `VectorizeBackgroundJob.ProcessBatch`
- **Execution**: Asynchronous, handled by Hangfire workers
- **Purpose**: Offload heavy processing from main thread

### Step 6: Update Document Status (Success)
**Location**: `PromptDocumentBusiness.cs:274-275`

```csharp
document.Action = DocumentAction.Vectorize_Success;
await _documentRepository.UpdateAsync(document);
```

## Background Job Processing

### VectorizeBackgroundJob.ProcessBatch
**Location**: `VectorizeBackgroundJob.cs:25-65`

This job runs asynchronously in Hangfire worker threads.

#### Step 1: Build Batch Request
**Location**: `VectorizeBackgroundJob.cs:29-45`

```csharp
var batchRequest = new BatchVectorizeRequestDto
{
    Items = chunks.Select(chunk => new VectorizeRequestDto
    {
        Text = chunk.FullText,  // Full hierarchical text
        Metadata = new Dictionary<string, object>
        {
            { "source_id", chunk.DocumentId },
            { "file_name", chunk.FileName },
            { "heading1", chunk.Heading1 },
            { "heading2", chunk.Heading2 },
            { "content", chunk.Content },
            { "tenant_id", tenantId },
            { "type", 1 }  // Document type
        }
    }).ToList()
};
```

- **Text**: FullText with all hierarchical context
- **Metadata**: Searchable/filterable fields stored with vector
- **tenant_id**: Enables multi-tenant filtering
- **source_id**: Links vector back to original document

#### Step 2: Call EmbeddingService API
**Location**: `VectorizeBackgroundJob.cs:47-48`

```csharp
var vectorizeUrl = $"{_appSettings.EmbeddingServiceUrl}/vectorize-batch";
var response = await PostAsync<BatchVectorizeRequestDto, VectorizeResponseDto>(vectorizeUrl, batchRequest);
```

- **Endpoint**: `POST /vectorize-batch`
- **Action**: Generate embeddings and store in Qdrant
- **Model**: `truro7/vn-law-embedding` (768 dimensions)
- **Collection**: `vn_law_documents`

#### EmbeddingService Processing

**Location**: `EmbeddingService/src/business.py:79-105`

For each item in the batch:
1. **Generate Embedding**: Convert text to 768-dim vector using `truro7/vn-law-embedding`
2. **Create Point**: Wrap vector with metadata and unique ID
3. **Upsert to Qdrant**: Store vector in `vn_law_documents` collection

```python
for item in items:
    embedding = self.encode_text(item.text)  # 768-dim vector
    point_id = str(uuid.uuid4())

    points.append(PointStruct(
        id=point_id,
        vector=embedding,
        payload={"text": item.text, **item.metadata}
    ))

self.qdrant_client.upsert(collection_name=collection_name, points=points)
```

#### Step 3: Handle Response
**Location**: `VectorizeBackgroundJob.cs:50-58`

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

## Document Status States

| State | Description | Location |
|-------|-------------|----------|
| `Upload` | Document uploaded to StorageService | After file upload |
| `Standardization` | Heading styles applied to .docx | During upload |
| `Vectorize_Start` | Vectorization process started | Before chunking |
| `Vectorize_Success` | All chunks embedded successfully | After all jobs complete |
| `Vectorize_Failed` | Vectorization failed | On error |

## Delete Document Flow

**Endpoint**: `DELETE /web-api/document/{id}`
**Location**: `PromptDocumentBusiness.cs:177-225`

### Two-Phase Deletion

#### Phase 1: Delete from SQL Database
```csharp
await _documentRepository.DeleteAsync(document);  // Soft delete
```

#### Phase 2: Delete Vectors from Qdrant
**Location**: `PromptDocumentBusiness.cs:193-216`

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

**EmbeddingService Deletion**: `EmbeddingService/src/business.py:107-122`

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

- **Filter**: Matches all vectors with source_id, tenant_id, and type
- **Result**: Removes all chunks from the document
- **Consistency**: Soft consistency - logs error but doesn't rollback DB if vector delete fails

## Configuration

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

### Regex Patterns

| Pattern | Matches | Example |
|---------|---------|---------|
| `RegexHeading1` | Chapters with Roman numerals | "CHƯƠNG I: QUY ĐỊNH CHUNG" |
| `RegexHeading2` | Sections with numbers | "Mục 1: Phạm vi điều chỉnh" |
| `RegexHeading3` | Articles with numbers | "Điều 1. Định nghĩa" |

## Performance Considerations

- **Chunking**: ~100-500ms per document (depends on size)
- **Batch Size**: 10 chunks per job (configurable)
- **Embedding Generation**: ~100-300ms per chunk
- **Total Time**: Asynchronous, doesn't block user request
- **Hangfire**: Processes jobs in background worker threads

## Example: Complete Document Processing

### Input Document Structure
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

### Extracted Chunks (3 chunks)

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

### Processing Flow

1. User uploads document → DocumentId: 123
2. Document status: `Upload` → `Vectorize_Start`
3. Extract 3 chunks with hierarchical structure
4. Create 1 batch (3 chunks < 10)
5. Enqueue 1 Hangfire job
6. Document status: `Vectorize_Success`
7. Background job calls EmbeddingService
8. 3 vectors stored in Qdrant with metadata
9. Vectors available for ChatProcessor RAG queries

---

## External Dependencies

### 1. EmbeddingService (Port 8000)
- **Endpoint**: `POST /embed`
- **Model**: `truro7/vn-law-embedding`
- **Output**: 768-dimensional embeddings
- **Purpose**: Convert text to vectors for semantic search

### 2. Qdrant Vector Database (Port 6333)
- **Collection**: `vn_law_documents`
- **Vector Dimension**: 768
- **Distance Metric**: Cosine similarity
- **Purpose**: Store and retrieve document vectors with multi-tenant filtering

### 3. Ollama LLM Engine (Port 11434)
- **Model**: `ontocord/vistral:latest`
- **Size**: 7B parameters (Q4_0 quantized)
- **Language**: Vietnamese-optimized
- **Purpose**: Generate natural language responses

### 4. RabbitMQ Message Broker (Port 5672) [Optional]
- **Input Queue**: `UserPromptReceived`
- **Output Queue**: `BotResponseCreated`
- **Purpose**: Async message processing for production

## Key Features

### Multi-Tenancy Support - Dual Query Architecture
**Implementation**: `src/business.py:107-121, 154-175`

#### Exact Tenant Search Method
```python
async def search_exact_tenant(self, query_vector: List[float], tenant_id: int, limit: int = 1):
    """Search for documents with exact tenant_id match"""
    search_filter = Filter(
        must=[
            FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id))
        ]
    )
    results = self.client.search(
        collection_name=self.collection_name,
        query_vector=query_vector,
        query_filter=search_filter,
        limit=limit
    )
    return results
```

#### Dual Query Execution
- **Query A**: Searches tenant_id=1 (State law - shared across all tenants)
- **Query B**: Searches tenant_id=current_user (Company-specific regulations)
- **Filter Type**: `must` (exact match) - not `should` (OR)
- **Execution**: Parallel using `asyncio.gather` for performance
- **Guarantee**: Always attempts to retrieve from BOTH sources

#### Legacy Method (Deprecated for Chat Processing)
```python
# Old approach - still available but not used in main chat flow
search_filter = Filter(
    should=[  # OR condition - can return all from one source
        FieldCondition(key="tenant_id", match=MatchValue(value=1)),
        FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id))
    ]
)
```

#### Benefits of Dual Query Architecture
1. **Guaranteed Cross-Reference**: Always retrieves from both legal base and company rules
2. **No Vector Bias**: Similarity scores can't cause one source to dominate results
3. **Compliance Focus**: Ensures LLM can compare company rules against legal requirements
4. **Clear Attribution**: Each document labeled with its source type
5. **Graceful Degradation**: System continues if one source has no matches

### Stateless Processing
- Each message processed independently
- No conversation history maintained
- No context from previous messages
- Simplifies scaling and reduces memory usage

### Conditional RAG Enhancement
- **Both sources found**: Adds labeled context from legal base and company rules
- **One source found**: Uses available source with appropriate label
- **No documents found**: Passes original message directly to LLM
- Graceful fallback ensures system always responds

## Complete Example: Dual-Query RAG Flow

### Scenario
**User**: Company employee (tenant_id=2) asks about overtime regulations

### Input Message
```json
{
  "conversation_id": 1,
  "user_id": 123,
  "message": "What are the overtime compensation rules?",
  "tenant_id": 2
}
```

### Processing Steps

#### 1. Generate Embedding
```python
query_embedding = [0.123, -0.456, 0.789, ...]  # 768 dimensions
```

#### 2. Execute Dual Queries in Parallel
```python
# Query A: State Law (tenant_id=1)
legal_base_results = search_exact_tenant(query_embedding, tenant_id=1, limit=1)
# → Returns: Điều 98. Làm thêm giờ...

# Query B: Company Rules (tenant_id=2)
company_rule_results = search_exact_tenant(query_embedding, tenant_id=2, limit=1)
# → Returns: Quy định tăng ca công ty...
```

#### 3. Build Labeled Context
```
Context information:
[STATE LAW]
Điều 98. Làm thêm giờ
1. Làm thêm giờ là thời gian làm việc vượt quá thời giờ làm việc bình thường...
2. Người sử dụng lao động trả lương làm thêm giờ như sau:
   a) Ngày thường: ít nhất bằng 150% tiền lương
   b) Ngày nghỉ hàng tuần: ít nhất bằng 200% tiền lương
   c) Ngày lễ, tết: ít nhất bằng 300% tiền lương

[COMPANY REGULATION]
Quy định về tăng ca
1. Nhân viên làm thêm giờ được hưởng:
   - Ngày thường: 160% lương cơ bản
   - Cuối tuần: 220% lương cơ bản
   - Lễ tết: 350% lương cơ bản
2. Phải đăng ký trước qua hệ thống HR

User question: What are the overtime compensation rules?

Please answer based on the context provided above. If both STATE LAW and COMPANY REGULATION are provided, compare and contrast them in your response.
```

#### 4. LLM Response
```
Về quy định tăng ca:

**Theo luật lao động (Điều 98):**
- Ngày thường: tối thiểu 150% lương
- Ngày nghỉ hàng tuần: tối thiểu 200% lương
- Ngày lễ, tết: tối thiểu 300% lương

**Quy định của công ty:**
- Ngày thường: 160% lương cơ bản (cao hơn mức tối thiểu)
- Cuối tuần: 220% lương cơ bản (cao hơn mức tối thiểu)
- Lễ tết: 350% lương cơ bản (cao hơn mức tối thiểu)
- Yêu cầu: Phải đăng ký trước qua hệ thống HR

**Kết luận:** Công ty đang áp dụng mức lương tăng ca cao hơn quy định pháp luật,
đảm bảo tuân thủ và có lợi cho người lao động.
```

#### 5. Output
```json
{
  "conversation_id": 1,
  "message": "Về quy định tăng ca...",
  "user_id": 0,
  "timestamp": "2025-12-17T10:30:45.123456",
  "model_used": "ontocord/vistral:latest",
  "rag_documents_used": 2,
  "source_ids": ["doc-state-law-98", "doc-company-overtime"]
}
```

### Key Observations
- **Both sources retrieved**: System guarantees cross-referencing
- **Clear labels**: User can see which is law vs company policy
- **Comparison enabled**: LLM explicitly compares compliance
- **2 documents tracked**: `rag_documents_used: 2` confirms dual retrieval
- **Source traceability**: `source_ids` array contains both document IDs

### Error Handling
- Comprehensive logging at each step
- Conversation ID tracking for debugging
- Health checks for all external services
- Graceful degradation on service failures

## Configuration

### Environment Variables (.env)

```bash
# RabbitMQ Configuration
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_QUEUE_INPUT=UserPromptReceived
RABBITMQ_QUEUE_OUTPUT=BotResponseCreated

# Ollama Configuration
OLLAMA_BASE_URL=http://localhost:11434
OLLAMA_MODEL=ontocord/vistral:latest
OLLAMA_TIMEOUT=300

# Qdrant Configuration
QDRANT_HOST=localhost
QDRANT_PORT=6333
QDRANT_COLLECTION=vn_law_documents
RAG_TOP_K=5

# Embedding Service Configuration
EMBEDDING_SERVICE_URL=http://localhost:8000

# FastAPI Configuration
FASTAPI_HOST=0.0.0.0
FASTAPI_PORT=8001

# Service Configuration
LOG_LEVEL=INFO
PREFETCH_COUNT=1
```

### Key Configuration Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `RAG_TOP_K` | 5 | Number of documents to retrieve from Qdrant |
| `OLLAMA_TIMEOUT` | 300 | Maximum seconds for LLM response generation |
| `EMBEDDING_SERVICE_URL` | http://localhost:8000 | EmbeddingService API endpoint |
| `FASTAPI_PORT` | 8001 | REST API port for direct testing |
| `QDRANT_COLLECTION` | vn_law_documents | Vector database collection name |

## Project Structure

```
ChatProcessor/
├── main.py                    # Entry point (RabbitMQ + FastAPI)
├── src/
│   ├── config.py             # Configuration settings
│   ├── business.py           # Core processing logic
│   │   ├── OllamaService     # Ollama LLM integration
│   │   ├── QdrantService     # Qdrant + Embedding integration
│   │   └── ChatBusiness      # Main processing orchestration
│   ├── consumer.py           # RabbitMQ consumer
│   ├── router.py             # FastAPI routes
│   └── schemas.py            # Message schemas
├── app/                      # Alternative app structure
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

## Running the Service

### Prerequisites
1. Python 3.11+
2. EmbeddingService running on port 8000
3. Qdrant running on port 6333 with `vn_law_documents` collection
4. Ollama running on port 11434 with `ontocord/vistral:latest` model
5. RabbitMQ running on port 5672 (optional for testing)

### Start Service

```bash
cd Services/ChatProcessor
python main.py
```

Service logs will show:
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

### Test with REST API

```bash
curl -X POST "http://localhost:8001/api/chat/test" \
  -H "Content-Type: application/json" \
  -d '{
    "conversation_id": 1,
    "message": "What are Vietnamese labor law regulations?",
    "user_id": 123,
    "tenant_id": 2
  }'
```

### Health Check

```bash
curl http://localhost:8001/health
```

Response:
```json
{
  "status": "healthy",
  "ollama": true,
  "qdrant": true
}
```

## Performance Considerations

- **Embedding Generation**: ~100-300ms (depends on text length)
- **Vector Search**: ~10-50ms (depends on collection size)
- **LLM Generation**: 5-60 seconds (depends on response length and model speed)
- **Total Processing Time**: Typically 5-60 seconds per message

## Troubleshooting

### Vector Dimension Mismatch
**Error**: `expected dim: 768, got 384`
- Ensure EmbeddingService is running and accessible
- Verify `EMBEDDING_SERVICE_URL` points to correct service

### Ollama 404 Error
**Error**: `Ollama error: 404`
- Check model name matches available models: `curl http://localhost:11434/api/tags`
- Pull model if missing: `ollama pull ontocord/vistral:latest`

### Collection Not Found
**Error**: `Collection 'vn_law_documents' doesn't exist`
- Create collection via DocumentService or EmbeddingService
- Update `QDRANT_COLLECTION` in .env to match existing collection

### Connection Refused
- Verify all services are running:
  - `curl http://localhost:8000/health` (EmbeddingService)
  - `curl http://localhost:6333/collections` (Qdrant)
  - `curl http://localhost:11434/api/tags` (Ollama)
  - `rabbitmqctl status` (RabbitMQ)

## Architecture Upgrade History

### v1.1.0 - Dual Query Cross-Referencing (2025-12-17)

#### Motivation
The original single-query approach using `Filter(should=[...])` (OR logic) did not guarantee retrieval from both legal base and company rules. Vector similarity scores could bias results toward one source, preventing effective compliance checking.

#### Changes Made

**Before (Single Query)**:
```python
# Could return 2 law chunks OR 2 company chunks
search_filter = Filter(
    should=[
        FieldCondition(key="tenant_id", match=MatchValue(value=1)),
        FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id))
    ]
)
rag_results = await qdrant_service.search_with_tenant_filter(
    query_vector=query_embedding, tenant_id=tenant_id, limit=5
)
```

**After (Dual Query)**:
```python
# Guarantees retrieval from BOTH sources
legal_base_task = qdrant_service.search_exact_tenant(
    query_vector=query_embedding, tenant_id=1, limit=1
)
company_rule_task = qdrant_service.search_exact_tenant(
    query_vector=query_embedding, tenant_id=tenant_id, limit=1
)
legal_base_results, company_rule_results = await asyncio.gather(
    legal_base_task, company_rule_task, return_exceptions=True
)
```

#### New Components
1. **`search_exact_tenant()` method** - Uses `must` filter for exact tenant_id match
2. **Parallel execution** - `asyncio.gather` for optimal performance
3. **Labeled context** - `[STATE LAW]` and `[COMPANY REGULATION]` tags
4. **Comparison instruction** - LLM prompted to compare and contrast sources
5. **Graceful degradation** - Handles missing results from either source

#### Benefits
- ✅ Guaranteed cross-referencing when documents exist in both sources
- ✅ No bias from vector similarity scores
- ✅ Clear source attribution in responses
- ✅ Better compliance checking capabilities
- ✅ Improved answer quality for legal/regulation queries

#### Migration Notes
- **Backward compatible**: Legacy `search_with_tenant_filter()` method still available
- **No breaking changes**: API interface unchanged
- **Performance**: Parallel queries maintain similar response times
- **Monitoring**: Track `rag_documents_used` field - should typically be 2

## Future Enhancements

1. **Conversation History**: Maintain context across multiple messages
2. **Caching**: Cache embeddings and LLM responses for common queries
3. **Streaming Responses**: Stream LLM output for better UX
4. **Hybrid Search**: Combine vector search with keyword search
5. **Response Quality Scoring**: Track and improve response accuracy
6. **A/B Testing**: Compare different prompting strategies
7. **Dynamic Source Weighting**: Adjust retrieval based on query type
8. **Multi-source Expansion**: Support more than 2 document sources

---

**Last Updated**: 2025-12-17
**Version**: 1.1.0 - Dual Query Cross-Referencing Architecture
