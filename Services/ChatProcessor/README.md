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

# OpenAI API Key (required for Ragas evaluation)
OPENAI_API_KEY=sk-your-openai-api-key-here
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

## Evaluation Batch API

The service includes a batch evaluation system that scores RAG responses using the [Ragas](https://docs.ragas.io/) framework.

### Overview

The evaluation system automatically scores chat interactions based on:
- **Faithfulness**: Measures factual consistency of answers with retrieved contexts
- **Answer Relevancy**: Measures how relevant answers are to questions

### Prerequisites

1. **OpenAI API Key** (required by Ragas for evaluation metrics)
   - Get your API key from [OpenAI Platform](https://platform.openai.com/api-keys)
   - Add to `.env` file:
     ```env
     OPENAI_API_KEY=sk-your-actual-api-key-here
     ```

2. **Evaluation logs file** (`evaluation_logs.json`)
   - Automatically generated when you use the `/api/chat/test` endpoint
   - Contains questions, contexts, and answers from chat interactions

### Input File Format

The system reads from `evaluation_logs.json` with this structure:

```json
[
  {
    "question": "What is the overtime rate for regular weekdays?",
    "contexts": [
      "Company policy text chunk 1...",
      "Legal framework text chunk 2..."
    ],
    "answer": "According to company policy, the overtime rate...",
    "conversation_id": 6,
    "user_id": 10,
    "tenant_id": 6,
    "timestamp": "2025-12-19T04:02:16.346785"
  }
]
```

### Running Evaluation

#### Option 1: Via API Endpoint (Recommended)

Start the FastAPI server:

```bash
python -m app.main
```

Make a POST request to the evaluation endpoint:

```bash
curl -X POST http://localhost:8000/evaluate-batch
```

Or using Python:

```python
import requests
response = requests.post("http://localhost:8000/evaluate-batch")
print(response.json())
```

#### Option 2: Programmatically

```python
from src.evaluation_service import get_evaluation_service

# Create evaluation service
service = get_evaluation_service(
    input_file="evaluation_logs.json",
    output_file="chat_logs_scored.json"
)

# Run evaluation
summary = await service.run_evaluation()
print(summary)
```

### How It Works

The evaluation process (`/evaluate-batch` endpoint):

1. **Loads** chat logs from `evaluation_logs.json`
2. **Filters** entries missing `ragas_score` (incremental evaluation)
3. **Converts** to HuggingFace Dataset format
4. **Evaluates** using Ragas metrics (faithfulness + answer_relevancy)
5. **Calculates** average score: `(faithfulness + answer_relevancy) / 2`
6. **Saves** scored results to `chat_logs_scored.json`

### Output Format

Results are saved to `chat_logs_scored.json` with added scoring fields:

```json
[
  {
    "question": "...",
    "contexts": [...],
    "answer": "...",
    "conversation_id": 6,
    "user_id": 10,
    "tenant_id": 6,
    "timestamp": "2025-12-19T04:02:16.346785",
    "ragas_score": 0.85,
    "faithfulness": 0.90,
    "answer_relevancy": 0.80
  }
]
```

### API Response

Success response:

```json
{
  "processed": 5,
  "file_saved": "chat_logs_scored.json",
  "message": "Successfully evaluated 5 entries"
}
```

No unevaluated entries:

```json
{
  "processed": 0,
  "file_saved": "chat_logs_scored.json",
  "message": "No unevaluated entries found"
}
```

### Error Handling

If OpenAI API key is missing:

```json
{
  "detail": "The api_key client option must be set either by passing api_key to the client or by setting the OPENAI_API_KEY environment variable"
}
```

**Solution**: Add your OpenAI API key to the `.env` file.

### Important Notes

- Only processes entries **without** `ragas_score` (avoids re-evaluation)
- Input: `evaluation_logs.json` (auto-generated from chat interactions)
- Output: `chat_logs_scored.json` (separate file to prevent locking)
- Requires valid OpenAI API key for Ragas metrics
- Evaluation uses OpenAI's GPT models, which incurs API costs

### Cost Considerations

Ragas evaluation uses OpenAI API calls:
- Each evaluation requires multiple LLM calls per entry
- Monitor your OpenAI usage at [OpenAI Platform](https://platform.openai.com/usage)
- Consider batch processing during off-peak hours

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
