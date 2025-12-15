# Document Vectorization Implementation Summary

## Overview

This implementation provides a complete end-to-end solution for processing Vietnamese legal documents with hierarchical chunking and vector storage.

## Architecture

```
┌─────────────────┐      ┌──────────────────┐      ┌─────────────────┐
│  DocumentService│─────>│ EmbeddingService │─────>│     Qdrant      │
│     (C#/.NET)   │      │     (Python)     │      │  Vector Store   │
└─────────────────┘      └──────────────────┘      └─────────────────┘
```

## Workflow

### 1. Document Upload and Standardization

**Endpoint**: `POST /web-api/document/upload`

The DocumentService receives a Word document and:
1. Creates a database record with status `Upload`
2. Standardizes headings:
   - "Chương" → Heading1 style
   - "Mục" or "Điều" → Heading2 style
3. Updates status to `Standardization`
4. Uploads to StorageService
5. Returns document ID

**Code Location**: `Services/DocumentService/Features/PromptDocumentBusiness.cs:31`

### 2. Hierarchical Chunking

**Endpoint**: `POST /web-api/document/vectorize/{documentId}`

The DocumentService implements a state-machine approach to parse documents:

#### State Variables
- `CurrentHeading1`: Tracks the current "Chương" (Chapter)
- `CurrentHeading2`: Tracks the current "Mục" (Section) or "Điều" (Article)
- `ContentParagraphs`: Accumulates content paragraphs

#### Processing Logic

```
FOR each paragraph in document:
  IF paragraph matches "Chương":
    → Flush current chunk (if content exists)
    → Update CurrentHeading1
    → Reset CurrentHeading2
    → Clear content buffer

  ELSE IF paragraph matches "Mục" or "Điều":
    → Flush current chunk (if content exists)
    → Update CurrentHeading2
    → Handle sequential Mục→Điều by concatenating
    → Clear content buffer

  ELSE (regular content):
    → Add to content buffer

END FOR
→ Flush final chunk
```

#### Chunk Structure

Each chunk contains:
```json
{
  "Heading1": "Chương I. NHỮNG QUY ĐỊNH CHUNG",
  "Heading2": "Mục 1. QUY ĐỊNH CHUNG\nĐiều 1. Phạm vi điều chỉnh",
  "Content": "Luật này quy định về...",
  "FullText": "[Heading1]\n[Heading2]\n[Content]",
  "DocumentId": 123,
  "FileName": "example.docx"
}
```

**Code Location**: `Services/DocumentService/Features/PromptDocumentBusiness.cs:135`

### 3. Vectorization and Storage

The DocumentService:
1. Downloads the file from StorageService
2. Extracts hierarchical chunks
3. Prepares batch request with metadata:
   ```json
   {
     "items": [
       {
         "text": "[FullText from chunk]",
         "metadata": {
           "document_id": 123,
           "file_name": "example.docx",
           "heading1": "Chương I...",
           "heading2": "Điều 1...",
           "content": "Actual content...",
           "tenant_id": "tenant-uuid"
         }
       }
     ]
   }
   ```
4. Sends to EmbeddingService `/vectorize-batch` endpoint
5. Updates document status to `Vectorize_Success` or `Vectorize_Failed`

**Code Location**: `Services/DocumentService/Features/PromptDocumentBusiness.cs:236`

### 4. Embedding Generation

The Python EmbeddingService:
1. Receives batch request
2. For each item:
   - Generates embedding using `truro7/vn-law-embedding` model
   - Creates a Qdrant point with UUID
   - Stores vector + metadata in Qdrant
3. Returns success status and count

**Code Location**: `Services/EmbeddingService/main.py:101`

## Key Features

### 1. Context Preservation

The chunking strategy ensures legal context is preserved:
- **Chapter level** (Chương): Top-level organization
- **Section/Article level** (Mục/Điều): Mid-level grouping
- **Content**: Actual legal text

When vectorized, each chunk includes the full hierarchy, allowing semantic search to understand the legal structure.

### 2. Flexible Heading Combinations

The system handles multiple heading patterns:
- Chương only
- Chương → Mục → Content
- Chương → Điều → Content
- Chương → Mục → Điều → Content (combines Mục and Điều)

### 3. No Empty Vectors

The `FlushChunk` method only creates chunks when content exists, preventing vectorization of standalone headers.

### 4. Batch Processing

Multiple chunks are sent in a single request to the EmbeddingService, improving performance.

### 5. Comprehensive Metadata

Each vector stores rich metadata for filtering and retrieval:
- Document ID
- File name
- Hierarchical headings
- Content text
- Tenant ID (for multi-tenancy)

## API Endpoints

### C# DocumentService

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/web-api/document/upload` | POST | Upload and standardize document |
| `/web-api/document/vectorize/{id}` | POST | Extract chunks and vectorize |

### Python EmbeddingService

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/embed` | POST | Generate embedding only |
| `/vectorize` | POST | Generate and store single vector |
| `/vectorize-batch` | POST | Generate and store multiple vectors |
| `/health` | GET | Service health check |

## Configuration

### C# DocumentService

Add to `appsettings.json`:
```json
{
  "AppSettings": {
    "StorageUrl": "http://localhost:5002",
    "EmbeddingServiceUrl": "http://localhost:8000"
  }
}
```

### Python EmbeddingService

Environment variables:
```bash
MODEL_NAME=truro7/vn-law-embedding
QDRANT_HOST=localhost
QDRANT_PORT=6333
QDRANT_COLLECTION=vn_law_documents
```

## Running the Stack

### Using Docker Compose

```bash
cd Services/EmbeddingService
docker-compose up -d
```

This starts:
- Qdrant on port 6333
- EmbeddingService on port 8000

### Standalone Services

1. **Start Qdrant**:
   ```bash
   docker run -p 6333:6333 qdrant/qdrant
   ```

2. **Start EmbeddingService**:
   ```bash
   cd Services/EmbeddingService
   pip install -r requirements.txt
   python main.py
   ```

3. **Start DocumentService**:
   ```bash
   cd Services/DocumentService
   dotnet run
   ```

## Testing the Implementation

### 1. Upload Document
```bash
curl -X POST http://localhost:5001/web-api/document/upload \
  -F "file=@legal_document.docx"
```

Response: `{ documentId: 123 }`

### 2. Vectorize Document
```bash
curl -X POST http://localhost:5001/web-api/document/vectorize/123
```

Response: `{ success: true, documentId: 123 }`

### 3. Verify in Qdrant
```bash
curl http://localhost:6333/collections/vn_law_documents
```

## Example Document Processing

**Input Document Structure**:
```
Chương I. NHỮNG QUY ĐỊNH CHUNG
  Mục 1. Phạm vi điều chỉnh
    Điều 1. Phạm vi
      Luật này quy định về...
    Điều 2. Đối tượng
      Luật này áp dụng cho...

  Mục 2. Giải thích từ ngữ
    Điều 3. Định nghĩa
      Trong luật này, các từ ngữ...
```

**Generated Chunks**:

Chunk 1:
```
Heading1: "Chương I. NHỮNG QUY ĐỊNH CHUNG"
Heading2: "Mục 1. Phạm vi điều chỉnh\nĐiều 1. Phạm vi"
Content: "Luật này quy định về..."
FullText: "Chương I. NHỮNG QUY ĐỊNH CHUNG\nMục 1. Phạm vi điều chỉnh\nĐiều 1. Phạm vi\nLuật này quy định về..."
```

Chunk 2:
```
Heading1: "Chương I. NHỮNG QUY ĐỊNH CHUNG"
Heading2: "Mục 1. Phạm vi điều chỉnh\nĐiều 2. Đối tượng"
Content: "Luật này áp dụng cho..."
FullText: "Chương I. NHỮNG QUY ĐỊNH CHUNG\nMục 1. Phạm vi điều chỉnh\nĐiều 2. Đối tượng\nLuật này áp dụng cho..."
```

Chunk 3:
```
Heading1: "Chương I. NHỮNG QUY ĐỊNH CHUNG"
Heading2: "Mục 2. Giải thích từ ngữ\nĐiều 3. Định nghĩa"
Content: "Trong luật này, các từ ngữ..."
FullText: "Chương I. NHỮNG QUY ĐỊNH CHUNG\nMục 2. Giải thích từ ngữ\nĐiều 3. Định nghĩa\nTrong luật này, các từ ngữ..."
```

## File Structure

```
AIChat2025/
├── Services/
│   ├── DocumentService/
│   │   ├── Features/
│   │   │   └── PromptDocumentBusiness.cs (Chunking logic)
│   │   ├── Endpoints/
│   │   │   └── DocumentEndpoint.cs (API endpoints)
│   │   ├── Dtos/
│   │   │   ├── DocumentChunkDto.cs
│   │   │   └── VectorizeRequestDto.cs
│   │   └── Config/
│   │       └── appsettings.json
│   └── EmbeddingService/
│       ├── main.py (FastAPI service)
│       ├── requirements.txt
│       ├── Dockerfile
│       ├── docker-compose.yml
│       ├── .env.example
│       └── README.md
└── Infrastructure/
    └── AppSettings.cs
```

## Next Steps

### Recommended Enhancements

1. **Search Functionality**: Add semantic search endpoint to query vectorized documents
2. **Background Processing**: Use message queue for async vectorization
3. **Monitoring**: Add telemetry and performance metrics
4. **Error Recovery**: Implement retry logic for failed vectorizations
5. **Pagination**: Handle large documents with pagination
6. **Caching**: Cache frequently accessed vectors

### Production Considerations

1. **Scalability**: Use Kubernetes for orchestration
2. **Security**: Add authentication and authorization
3. **Rate Limiting**: Implement rate limits on API endpoints
4. **Backup**: Regular backups of Qdrant data
5. **Monitoring**: Set up Prometheus/Grafana for metrics

## Troubleshooting

### Common Issues

1. **Document not vectorizing**
   - Check DocumentService logs
   - Verify EmbeddingServiceUrl in appsettings.json
   - Ensure EmbeddingService is running

2. **Empty chunks**
   - Verify document has content after headers
   - Check regex patterns match your document format

3. **Qdrant connection failed**
   - Ensure Qdrant is running on port 6333
   - Check QDRANT_HOST environment variable
   - Verify network connectivity

## Support

For issues or questions, check:
- DocumentService logs: `Services/DocumentService/logs/`
- EmbeddingService logs: `docker-compose logs embedding-service`
- Qdrant logs: `docker-compose logs qdrant`
