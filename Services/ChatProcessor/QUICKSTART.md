# ChatProcessor - Quick Start Guide

Get the ChatProcessor AI Worker Service up and running in 5 minutes!

## Prerequisites

Before you start, make sure you have:

1. **Python 3.11+** installed
   ```bash
   python --version  # Should be 3.11 or higher
   ```

2. **RabbitMQ** running
   ```bash
   # Check if RabbitMQ is running
   # Windows: Open Services and look for RabbitMQ
   # Linux/Mac: systemctl status rabbitmq-server
   ```

3. **Ollama** installed and running
   ```bash
   # Check Ollama
   curl http://localhost:11434/api/tags

   # Or visit: http://localhost:11434
   ```

4. **Ollama model** downloaded
   ```bash
   # Download llama2 (or your preferred model)
   ollama pull llama2

   # List available models
   ollama list
   ```

## Installation (Choose One)

### Option 1: Quick Start Script (Recommended)

#### Windows
```bash
cd Services\ChatProcessor
run.bat
```

#### Linux/Mac
```bash
cd Services/ChatProcessor
chmod +x run.sh
./run.sh
```

### Option 2: Manual Setup

1. **Create virtual environment**
   ```bash
   cd Services/ChatProcessor
   python -m venv venv
   ```

2. **Activate virtual environment**
   ```bash
   # Windows
   venv\Scripts\activate

   # Linux/Mac
   source venv/bin/activate
   ```

3. **Install dependencies**
   ```bash
   pip install -r requirements.txt
   ```

4. **Configure environment**
   ```bash
   # Copy example config
   cp .env.example .env

   # Edit .env with your settings (if needed)
   # nano .env  (Linux/Mac)
   # notepad .env  (Windows)
   ```

5. **Run the service**
   ```bash
   python -m app.main
   ```

### Option 3: Docker

```bash
# Build and run with Docker Compose
docker-compose up -d

# View logs
docker-compose logs -f chatprocessor

# Stop
docker-compose down
```

## Configuration

The default configuration works out-of-the-box for local development:

```env
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
OLLAMA_BASE_URL=http://localhost:11434
OLLAMA_MODEL=llama2
```

Only edit `.env` if you need to change these defaults.

## Testing

### Test Components
```bash
python test_service.py
```

### Test Full Integration

1. **Start the service**
   ```bash
   python -m app.main
   ```

2. **Publish a test message to RabbitMQ**

   Using RabbitMQ Management UI (http://localhost:15672):
   - Login: guest/guest
   - Go to "Queues" tab
   - Click on "UserPromptReceived" queue
   - Click "Publish message"
   - Paste this JSON:
   ```json
   {
     "conversation_id": 1,
     "message": "What is Python?",
     "user_id": 1,
     "timestamp": "2025-12-16T10:30:00Z"
   }
   ```
   - Click "Publish message"

3. **Check the response**
   - Go to "BotResponseCreated" queue
   - Click "Get messages"
   - You should see the AI response!

## Verify It's Working

You should see logs like this:

```
2025-12-16 10:30:00,123 - app.main - INFO - Starting ChatProcessor Service
2025-12-16 10:30:01,456 - app.services.rabbitmq_service - INFO - Successfully connected to RabbitMQ
2025-12-16 10:30:02,789 - app.main - INFO - All health checks passed
2025-12-16 10:30:03,012 - app.main - INFO - ChatProcessor is now running. Press Ctrl+C to stop.
```

When a message is processed:
```
2025-12-16 10:35:00,123 - app.main - INFO - [ConversationId: 1] Processing prompt from User 1: 'What is Python?...'
2025-12-16 10:35:05,456 - app.main - INFO - [ConversationId: 1] Generated response (length: 150)
2025-12-16 10:35:05,789 - app.main - INFO - [ConversationId: 1] Successfully published response to 'BotResponseCreated' queue
```

## Troubleshooting

### "Failed to connect to RabbitMQ"
- Make sure RabbitMQ is running: `netstat -an | find "5672"` (Windows) or `netstat -an | grep 5672` (Linux/Mac)
- Check credentials in `.env`

### "Failed to connect to Ollama"
- Make sure Ollama is running: `curl http://localhost:11434/api/tags`
- Start Ollama if needed: `ollama serve`

### "Model not found"
- Download the model: `ollama pull llama2`
- Or change `OLLAMA_MODEL` in `.env` to a model you have

### "Module not found" errors
- Make sure virtual environment is activated
- Reinstall dependencies: `pip install -r requirements.txt`

### Service crashes immediately
- Check logs in `chatprocessor.log`
- Run with debug logging: `LOG_LEVEL=DEBUG python -m app.main`

## Next Steps

1. **Integrate with .NET Service**: Make sure your .NET ChatService publishes to `UserPromptReceived` and listens to `BotResponseCreated`

2. **Production Deployment**: See README.md for systemd/Windows Service setup

3. **Monitoring**: Add monitoring and alerting for production use

4. **Scale**: Run multiple instances for high availability

## Need Help?

- Check the full README.md for detailed documentation
- Review logs in `chatprocessor.log`
- Check RabbitMQ Management UI at http://localhost:15672

## Summary

You now have:
- ✓ A Python AI Worker Service running
- ✓ Connected to RabbitMQ for message processing
- ✓ Integrated with Ollama for AI responses
- ✓ Ready to process user prompts!

The service will:
1. Listen to `UserPromptReceived` queue
2. Process each prompt with Ollama AI
3. Publish responses to `BotResponseCreated` queue
4. Include `conversation_id` for proper routing

**Happy coding!**
