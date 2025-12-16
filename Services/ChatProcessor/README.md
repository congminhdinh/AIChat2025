# ChatProcessor - AI Worker Service

Python-based AI worker service that processes user prompts with multi-tenant RAG using Qdrant vector database and Ollama LLM.

## Overview

Dual-interface service that processes chat messages through:
- **RabbitMQ Consumer**: Production message queue integration
- **FastAPI Endpoint**: Direct testing interface at `/api/chat/test`

Both interfaces use the same core processing function ensuring consistent behavior.

## Tech Stack

- Python 3.11+
- FastAPI + Uvicorn (REST API)
- aio_pika (RabbitMQ async client)
- Qdrant (Vector database for RAG)
- Ollama (LLM inference)
- Pydantic (data validation)

## Architecture

```
┌──────────────┐    UserPromptReceived     ┌───────────────┐
│  .NET Chat   │ ────────────────────────▶ │               │
│   Service    │                           │ ChatProcessor │
│              │ ◀──────────────────────── │   (Python)    │
└──────────────┘   BotResponseCreated      └───────┬───────┘
                                                   │
                                                   │
                   ┌───────────────────────────────┤
                   │                               │
                   │ Shared Core Function          │
                   │ (Multi-tenancy RAG)           │
                   │                               │
              ┌────▼────┐                    ┌────▼────┐
              │ Qdrant  │                    │ Ollama  │
              │ Vector  │                    │   LLM   │
              │   DB    │                    │  API    │
              └─────────┘                    └─────────┘
```

## Project Structure

```
ChatProcessor/
├── app/
│   ├── main.py              # Entry point with RabbitMQ + FastAPI
│   ├── api.py               # FastAPI endpoints
│   ├── config.py            # Configuration
│   ├── models/
│   │   └── messages.py      # Message schemas
│   └── services/
│       ├── service.py       # Shared core processing logic
│       ├── qdrant_service.py
│       ├── ollama_service.py
│       └── rabbitmq_service.py
├── requirements.txt
├── .env.example
└── README.md
```

## Installation

### Prerequisites

- Python 3.11+
- RabbitMQ server
- Ollama installed with model
- Qdrant vector database

### Setup

```bash
cd Services/ChatProcessor
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
pip install -r requirements.txt
cp .env.example .env
```

## Configuration

Create `.env` file:

```env
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_QUEUE_INPUT=UserPromptReceived
RABBITMQ_QUEUE_OUTPUT=BotResponseCreated

OLLAMA_BASE_URL=http://localhost:11434
OLLAMA_MODEL=llama2
OLLAMA_TIMEOUT=300

QDRANT_HOST=localhost
QDRANT_PORT=6333
QDRANT_COLLECTION=documents
RAG_TOP_K=5

FASTAPI_HOST=0.0.0.0
FASTAPI_PORT=8000

LOG_LEVEL=INFO
PREFETCH_COUNT=1
```

## Usage

### Run Service

```bash
python -m app.main
```

Service starts both:
- RabbitMQ consumer on `UserPromptReceived` queue
- FastAPI server on `http://0.0.0.0:8000`

### Test with FastAPI

```bash
curl -X POST http://localhost:8000/api/chat/test \
  -H "Content-Type: application/json" \
  -d '{
    "conversation_id": 1,
    "message": "What is machine learning?",
    "user_id": 123,
    "tenant_id": 2
  }'
```

### Health Check

```bash
curl http://localhost:8000/health
```

## Multi-Tenancy RAG Logic

The core processing function implements tenant-based document filtering:

```python
# Searches Qdrant with filter:
# tenant_id = 1 (Shared/System Data)
# OR tenant_id = input_tenant_id (Private Tenant Data)
```

This allows:
- **Tenant 1**: Global shared knowledge base
- **Tenant N**: Private tenant-specific documents

## Message Contracts

### Input (UserPromptReceivedMessage)

```json
{
  "conversation_id": 123,
  "message": "What is AI?",
  "user_id": 456,
  "tenant_id": 2,
  "timestamp": "2025-12-17T10:30:00Z"
}
```

### Output (BotResponseCreatedMessage)

```json
{
  "conversation_id": 123,
  "message": "AI is...",
  "user_id": 0,
  "timestamp": "2025-12-17T10:30:05Z",
  "model_used": "llama2"
}
```

## Development

### Run Tests

```bash
python test_service.py
```

### Docker

```bash
docker build -t chatprocessor:latest .
docker run -d --name chatprocessor --env-file .env --network host chatprocessor:latest
```

## Troubleshooting

### RabbitMQ Connection Failed
```bash
rabbitmqctl status
telnet localhost 5672
```

### Qdrant Connection Failed
```bash
curl http://localhost:6333/collections
```

### Ollama Not Responding
```bash
curl http://localhost:11434/api/tags
ollama list
```

## Production Notes

1. Use proper embedding model in `qdrant_service.py` (currently stub)
2. Configure dead letter queue for failed messages
3. Add monitoring/metrics (Prometheus)
4. Use secrets management for credentials
5. Enable horizontal scaling with multiple instances
6. Implement conversation history management
