# ChatProcessor - AI Worker Service

A dedicated Python-based AI worker service that processes user prompts from RabbitMQ and generates AI responses using Ollama.

## Overview

**Role**: This is a dedicated AI Worker Service. Its ONLY job is to process prompts and generate answers.

**Tech Stack**:
- Python 3.11+
- aio_pika (RabbitMQ async client)
- httpx (Ollama API client)
- Pydantic (data validation)

## Workflow

1. **Connect** to RabbitMQ and listen to the `UserPromptReceived` queue
2. **Process** every incoming message as a prompt
3. **Call** Ollama API (`POST /api/chat`) with the message content
4. **Publish** the AI-generated response to `BotResponseCreated` queue
5. **Include** the original `ConversationId` in the response so the .NET service knows which chat window to update

## Architecture

```
┌──────────────┐      UserPromptReceived     ┌───────────────┐
│              │ ─────────────────────────▶  │               │
│  .NET Chat   │                             │ ChatProcessor │
│   Service    │                             │   (Python)    │
│              │  ◀─────────────────────────  │               │
└──────────────┘     BotResponseCreated      └───────┬───────┘
                                                     │
                                                     │ HTTP POST
                                                     │ /api/chat
                                                     ▼
                                             ┌───────────────┐
                                             │  Ollama API   │
                                             │  (AI Model)   │
                                             └───────────────┘
```

## Project Structure

```
ChatProcessor/
├── app/
│   ├── __init__.py
│   ├── main.py              # Main entry point and orchestrator
│   ├── config.py            # Configuration management
│   ├── models/
│   │   ├── __init__.py
│   │   └── messages.py      # Pydantic models for RabbitMQ messages
│   └── services/
│       ├── __init__.py
│       ├── ollama_service.py    # Ollama API client
│       └── rabbitmq_service.py  # RabbitMQ consumer/publisher
├── requirements.txt
├── .env.example
├── Dockerfile
└── README.md
```

## Installation

### Prerequisites

- Python 3.11 or higher
- RabbitMQ server running
- Ollama installed and running

### Steps

1. **Clone the repository** (if not already done)

2. **Navigate to the ChatProcessor directory**:
   ```bash
   cd Services/ChatProcessor
   ```

3. **Create a virtual environment**:
   ```bash
   python -m venv venv
   ```

4. **Activate the virtual environment**:
   - Windows:
     ```bash
     venv\Scripts\activate
     ```
   - Linux/Mac:
     ```bash
     source venv/bin/activate
     ```

5. **Install dependencies**:
   ```bash
   pip install -r requirements.txt
   ```

6. **Configure environment variables**:
   ```bash
   cp .env.example .env
   # Edit .env with your configuration
   ```

## Configuration

Create a `.env` file with the following variables:

```env
# RabbitMQ Configuration
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_QUEUE_INPUT=UserPromptReceived
RABBITMQ_QUEUE_OUTPUT=BotResponseCreated

# Ollama Configuration
OLLAMA_BASE_URL=http://localhost:11434
OLLAMA_MODEL=llama2
OLLAMA_TIMEOUT=300

# Service Configuration
LOG_LEVEL=INFO
PREFETCH_COUNT=1
```

### Configuration Options

| Variable | Description | Default |
|----------|-------------|---------|
| `RABBITMQ_HOST` | RabbitMQ server host | `localhost` |
| `RABBITMQ_PORT` | RabbitMQ server port | `5672` |
| `RABBITMQ_USERNAME` | RabbitMQ username | `guest` |
| `RABBITMQ_PASSWORD` | RabbitMQ password | `guest` |
| `RABBITMQ_QUEUE_INPUT` | Input queue name | `UserPromptReceived` |
| `RABBITMQ_QUEUE_OUTPUT` | Output queue name | `BotResponseCreated` |
| `OLLAMA_BASE_URL` | Ollama API base URL | `http://localhost:11434` |
| `OLLAMA_MODEL` | Ollama model to use | `llama2` |
| `OLLAMA_TIMEOUT` | Request timeout (seconds) | `300` |
| `LOG_LEVEL` | Logging level | `INFO` |
| `PREFETCH_COUNT` | RabbitMQ prefetch count | `1` |

## Usage

### Running the Service

```bash
# Make sure virtual environment is activated
python -m app.main
```

Or use the module directly:

```bash
python -m app.main
```

### Running with Docker

```bash
# Build the image
docker build -t chatprocessor:latest .

# Run the container
docker run -d \
  --name chatprocessor \
  --env-file .env \
  --network host \
  chatprocessor:latest
```

### Running as a Background Service

#### Linux (systemd)

Create `/etc/systemd/system/chatprocessor.service`:

```ini
[Unit]
Description=ChatProcessor AI Worker Service
After=network.target rabbitmq-server.service

[Service]
Type=simple
User=your-user
WorkingDirectory=/path/to/Services/ChatProcessor
Environment="PATH=/path/to/Services/ChatProcessor/venv/bin"
ExecStart=/path/to/Services/ChatProcessor/venv/bin/python -m app.main
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Then:
```bash
sudo systemctl daemon-reload
sudo systemctl enable chatprocessor
sudo systemctl start chatprocessor
sudo systemctl status chatprocessor
```

#### Windows (NSSM)

1. Download NSSM from https://nssm.cc/
2. Install the service:
   ```bash
   nssm install ChatProcessor "C:\path\to\venv\Scripts\python.exe" "-m app.main"
   nssm set ChatProcessor AppDirectory "C:\path\to\Services\ChatProcessor"
   nssm start ChatProcessor
   ```

## Message Contracts

### Input Message (UserPromptReceived)

```json
{
  "conversation_id": 123,
  "message": "What is the capital of France?",
  "user_id": 456,
  "timestamp": "2025-12-16T10:30:00Z"
}
```

### Output Message (BotResponseCreated)

```json
{
  "conversation_id": 123,
  "message": "The capital of France is Paris.",
  "user_id": 0,
  "timestamp": "2025-12-16T10:30:05Z",
  "model_used": "llama2"
}
```

**CRITICAL**: The `conversation_id` MUST be included in the response so the .NET service knows which chat window to update.

## Logging

Logs are written to:
- **Console** (stdout) - for real-time monitoring
- **File** (`chatprocessor.log`) - for persistent logging

Log format:
```
2025-12-16 10:30:00,123 - app.main - INFO - [ConversationId: 123] Processing prompt...
```

## Error Handling

The service implements robust error handling:

- **Connection Errors**: Automatic reconnection with exponential backoff (aio_pika feature)
- **Ollama Timeouts**: Configurable timeout with proper error messages
- **Message Processing Errors**: Logged and rejected (not requeued)
- **Graceful Shutdown**: Handles SIGTERM/SIGINT signals

## Health Checks

The service performs health checks on startup:
- **RabbitMQ**: Verifies connection and channel are open
- **Ollama**: Checks if API is reachable

## Development

### Running Tests

```bash
# Install dev dependencies
pip install pytest pytest-asyncio pytest-cov

# Run tests
pytest tests/ -v --cov=app
```

### Code Style

This project follows PEP 8 and uses snake_case for naming conventions.

```bash
# Format code
pip install black isort
black app/
isort app/

# Lint code
pip install flake8
flake8 app/
```

## Troubleshooting

### Issue: Cannot connect to RabbitMQ

**Solution**: Verify RabbitMQ is running and credentials are correct:
```bash
# Check RabbitMQ status
rabbitmqctl status

# Test connection
telnet localhost 5672
```

### Issue: Cannot connect to Ollama

**Solution**: Verify Ollama is running:
```bash
# Check Ollama
curl http://localhost:11434/api/tags

# List available models
ollama list
```

### Issue: Messages not being consumed

**Solution**: Check queue exists and has messages:
```bash
# List queues
rabbitmqadmin list queues

# Check message count
rabbitmqadmin list queues name messages
```

### Issue: Slow response times

**Solution**:
- Check Ollama model performance
- Increase `OLLAMA_TIMEOUT` if needed
- Monitor system resources (CPU, RAM, GPU)

## Production Considerations

1. **Monitoring**: Implement health check endpoints
2. **Metrics**: Add Prometheus metrics for monitoring
3. **Dead Letter Queue**: Configure DLQ for failed messages
4. **Conversation History**: Implement conversation history management
5. **Rate Limiting**: Add rate limiting for Ollama API calls
6. **Load Balancing**: Run multiple instances for high availability
7. **Secrets Management**: Use proper secrets management (Azure Key Vault, AWS Secrets Manager, etc.)

## Contributing

Follow these guidelines:
1. Use snake_case for Python code
2. Add type hints to all functions
3. Write docstrings for all public methods
4. Add unit tests for new features
5. Update README for significant changes

## License

[Your License Here]

## Support

For issues and questions, please contact the development team or create an issue in the repository.
