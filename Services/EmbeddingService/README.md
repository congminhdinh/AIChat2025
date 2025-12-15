# VN Law Embedding Service

A FastAPI-based microservice for generating Vietnamese law-specific text embeddings and storing them in Qdrant vector database.

## Features

- **Text Embedding**: Generate embeddings using the `truro7/vn-law-embedding` model
- **Qdrant Integration**: Direct vector storage in Qdrant vector database
- **Batch Processing**: Support for batch vectorization operations
- **Health Monitoring**: Health check endpoint for service monitoring

## API Endpoints

### 1. Generate Embedding
Returns vector embeddings for the provided text.

```http
POST /embed
Content-Type: application/json

{
  "text": "Your text here"
}
```

**Response:**
```json
{
  "vector": [0.123, 0.456, ...],
  "dimensions": 768
}
```

### 2. Vectorize and Store
Generates embedding and stores directly in Qdrant.

```http
POST /vectorize
Content-Type: application/json

{
  "text": "Your text here",
  "metadata": {
    "document_id": 1,
    "file_name": "example.docx",
    "heading1": "Chương I",
    "heading2": "Điều 1"
  },
  "collection_name": "vn_law_documents"
}
```

**Response:**
```json
{
  "success": true,
  "point_id": "uuid-here",
  "dimensions": 768,
  "collection": "vn_law_documents"
}
```

### 3. Batch Vectorize
Process multiple items in a single request.

```http
POST /vectorize-batch
Content-Type: application/json

{
  "items": [
    {
      "text": "Text 1",
      "metadata": {"key": "value"}
    },
    {
      "text": "Text 2",
      "metadata": {"key": "value"}
    }
  ],
  "collection_name": "vn_law_documents"
}
```

**Response:**
```json
{
  "success": true,
  "count": 2,
  "collection": "vn_law_documents"
}
```

### 4. Health Check
Check service status.

```http
GET /health
```

**Response:**
```json
{
  "status": "ok",
  "model": "truro7/vn-law-embedding",
  "qdrant": "localhost:6333"
}
```

## Quick Start

### Option 1: Docker Compose (Recommended)

1. Start all services (Qdrant + Embedding Service):
```bash
cd Services/EmbeddingService
docker-compose up -d
```

2. Check logs:
```bash
docker-compose logs -f
```

3. Stop services:
```bash
docker-compose down
```

### Option 2: Run Locally

1. Start Qdrant:
```bash
docker run -p 6333:6333 -p 6334:6334 \
  -v $(pwd)/qdrant_data:/qdrant/storage \
  qdrant/qdrant
```

2. Install dependencies:
```bash
pip install -r requirements.txt
```

3. Run the service:
```bash
python main.py
```

### Option 3: Docker Only

1. Build the image:
```bash
docker build -t embedding-service .
```

2. Run the container:
```bash
docker run -p 8000:8000 \
  -e QDRANT_HOST=host.docker.internal \
  -e QDRANT_PORT=6333 \
  embedding-service
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `MODEL_NAME` | `truro7/vn-law-embedding` | SentenceTransformer model name |
| `QDRANT_HOST` | `localhost` | Qdrant server host |
| `QDRANT_PORT` | `6333` | Qdrant server port |
| `QDRANT_COLLECTION` | `vn_law_documents` | Default collection name |

## Integration with C# DocumentService

The C# DocumentService uses this embedding service to vectorize legal documents:

1. **Upload Document**: Document is uploaded to DocumentService
2. **Standardization**: Headers are standardized (Chương, Mục, Điều)
3. **Hierarchical Chunking**: Document is parsed into contextual chunks
4. **Vectorization**: Chunks are sent to this service via `/vectorize-batch`
5. **Storage**: Vectors are stored in Qdrant with metadata

### Example C# Usage

```csharp
// Call from DocumentService
POST /web-api/document/vectorize/{documentId}
```

This will:
- Extract hierarchical chunks from the document
- Send them to the embedding service
- Store vectors in Qdrant with full metadata

## Dependencies

- **fastapi**: Web framework
- **uvicorn**: ASGI server
- **sentence-transformers**: Text embedding models
- **torch**: PyTorch for model inference
- **qdrant-client**: Qdrant vector database client
- **langchain**: LangChain framework
- **langchain-community**: LangChain community integrations

## API Documentation

Once running, visit:
- Swagger UI: `http://localhost:8000/docs`
- ReDoc: `http://localhost:8000/redoc`

## Qdrant Dashboard

Access Qdrant dashboard at: `http://localhost:6333/dashboard`

## Troubleshooting

### Model Download Issues
If the model fails to download, ensure you have internet connectivity and sufficient disk space.

### Qdrant Connection Issues
- Ensure Qdrant is running: `docker ps | grep qdrant`
- Check Qdrant logs: `docker logs qdrant`
- Verify port 6333 is not in use by another service

### Performance Optimization
- Use batch endpoints for multiple items
- Consider GPU acceleration for faster inference
- Adjust Qdrant configuration for production workloads
