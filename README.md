# AIChat2025

A **Multitenancy RAG Application** for answering Company Rules with comparison to Vietnam Law. This system uses Retrieval-Augmented Generation (RAG) to provide accurate responses based on company policies while cross-referencing with Vietnamese legal documents.

---

## Prerequisites

- .NET 9 SDK
- Python 3.11+
- SQL Server
- RabbitMQ
- Minio (for storage)
- Ollama (for LLM)
- Qdrant (for vector database)

---

## Infrastructure Setup

### 1. Ollama Setup (for ChatProcessor)

Ollama is used by the ChatProcessor service for LLM inference.

```bash
# Install Ollama from https://ollama.ai

# Pull the Vietnamese LLM model (Vistral 7B)
ollama pull ontocord/vistral:latest

# Verify Ollama is running
curl http://localhost:11434/api/tags
```

**Configuration** (in `Services/ChatProcessor/.env`):
```env
OLLAMA_BASE_URL=http://localhost:11434
OLLAMA_MODEL=ontocord/vistral:latest
OLLAMA_TIMEOUT=300
```

### 2. RabbitMQ Setup (for ChatProcessor & ChatService)

RabbitMQ is used for message queuing between ChatService and ChatProcessor.

```bash
# Using Docker
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management

# Access management UI at http://localhost:15672
# Default credentials: guest/guest
```

**Configuration for ChatService** (`Services/ChatService/Config/appsettings.Development.json`):
```json
{
  "RabbitMQEndpoint": "localhost:5672",
  "RabbitMQUsername": "guest",
  "RabbitMQPassword": "guest"
}
```

**Configuration for ChatProcessor** (`Services/ChatProcessor/.env`):
```env
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_QUEUE_INPUT=UserPromptReceived
RABBITMQ_QUEUE_OUTPUT=BotResponseCreated
```

### 3. Qdrant Setup (for EmbeddingService)

Qdrant is used as the vector database for storing document embeddings.

```bash
# Using Docker
docker run -d --name qdrant \
  -p 6333:6333 \
  -p 6334:6334 \
  qdrant/qdrant

# Access dashboard at http://localhost:6333/dashboard
```

**Configuration** (`Services/EmbeddingService/.env`):
```env
QDRANT_HOST=localhost
QDRANT_PORT=6333
QDRANT_COLLECTION=vn_law_documents
MODEL_NAME=truro7/vn-law-embedding
```

### 4. Minio Setup (for StorageService)

Minio is used for document storage.

```bash
# Using Docker
docker run -d --name minio \
  -p 9000:9000 \
  -p 9001:9001 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  minio/minio server /data --console-address ":9001"

# Access console at http://localhost:9001
# Create bucket: ai-chat-2025
```

**Configuration** (`Services/StorageService/Config/appsettings.Development.json`):
```json
{
  "MinioEndpoint": "localhost:9000",
  "MinioAccessKey": "minioadmin",
  "MinioSecretKey": "minioadmin",
  "MinioBucket": "ai-chat-2025",
  "DocumentFilePath": "G:\\Storages"
}
```

---

## Configuration Files

Update the following configuration files before running:

### appsettings.json (Production)
- `Services/ChatService/Config/appsettings.json` - RabbitMQ, Database connection
- `Services/StorageService/Config/appsettings.json` - Minio settings

### appsettings.Development.json (Development)
- `Services/ChatService/Config/appsettings.Development.json` - Local RabbitMQ settings
- `Services/StorageService/Config/appsettings.Development.json` - Local Minio settings
- `Services/AccountService/Config/appsettings.Development.json` - Database connection
- `Services/TenantService/Config/appsettings.Development.json` - Database connection
- `Services/DocumentService/Config/appsettings.Development.json` - Database connection

### Python Services (.env files)
- `Services/ChatProcessor/.env` - Ollama, RabbitMQ, Qdrant settings
- `Services/EmbeddingService/.env` - Qdrant, embedding model settings

---

## Local Development Setup

### 1. C# Services (dotnet)

```bash
# Restore and build all projects
dotnet restore
dotnet build

# Run individual services (each in separate terminal)
dotnet run --project ApiGateway
dotnet run --project Services/AccountService
dotnet run --project Services/TenantService
dotnet run --project Services/DocumentService
dotnet run --project Services/ChatService
dotnet run --project Services/StorageService
dotnet run --project AdminCMS
dotnet run --project WebApp
```

### 2. Python Services (venv)

#### EmbeddingService
```bash
cd Services/EmbeddingService
python -m venv venv
venv\Scripts\activate      # Windows
# source venv/bin/activate  # Linux/Mac
pip install -r requirements.txt
python main.py
```

#### ChatProcessor
```bash
cd Services/ChatProcessor
python -m venv venv
venv\Scripts\activate      # Windows
# source venv/bin/activate  # Linux/Mac
pip install -r requirements.txt
python main.py
```

---

## Service Ports (Default)

| Service           | Port  | Description                    |
|-------------------|-------|--------------------------------|
| ApiGateway        | 5002  | API Gateway                    |
| AdminCMS          | 7263  | Admin Management Portal        |
| WebApp            | 7262  | User-facing Web Application    |
| EmbeddingService  | 8000  | Python embedding service       |
| ChatProcessor     | 8001  | Python chat processing service |

---

## Accessing the Application

- **WebApp**: http://localhost:7262 - User interface for chatting and querying company rules
- **AdminCMS**: http://localhost:7263 - Admin portal for managing tenants, documents, and configurations

---

## Model Information

| Purpose    | Model Name                  | Description                              |
|------------|-----------------------------|------------------------------------------|
| LLM        | `ontocord/vistral:latest`   | Vistral 7B - Vietnamese language model   |
| Embedding  | `truro7/vn-law-embedding`   | Vietnamese law embedding model for RAG   |
