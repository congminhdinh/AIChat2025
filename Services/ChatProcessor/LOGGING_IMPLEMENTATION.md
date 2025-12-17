# ChatProcessor Logging Implementation

## Overview

Comprehensive logging system implemented for ChatProcessor, mirroring the EmbeddingService logging pattern with TimedRotatingFileHandler and session-based tracking.

## Implementation Summary

### 1. Created `src/logger.py`

**Purpose**: Centralized logger setup with session context management

**Key Features**:
- **TimedRotatingFileHandler**: Daily rotation at midnight
- **Retention**: 30 days of logs (`backupCount=30`)
- **Log Directory**: `Services/ChatProcessor/logs/`
- **Log File**: `chatprocessor.log`
- **Session Tracking**: Context-based session ID using `ContextVar`

**Log Format**:
```
{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SessionId} {Message}{NewLine}{Exception}
```

**Example Log Entry**:
```
2025-12-17 16:45:23.456 [INF] a1b2c3d4 Request: POST /api/chat/test | Query: None | Body: {"conversation_id":1,"message":"Hello"}
2025-12-17 16:45:28.789 [INF] a1b2c3d4 Response: 200 | Process Time: 5.333s
```

**Functions Exported**:
- `logger`: Main logger instance
- `set_session_id(session_id: str)`: Set session ID for current context
- `clear_session_id()`: Clear session ID after request/message
- `get_session_id()`: Get or create session ID

### 2. Updated `main.py`

**Changes**:
- Removed old `logging.basicConfig()` configuration
- Imported new logger and session management functions
- Added FastAPI middleware for HTTP request/response logging

**Middleware Implementation**:
```python
@app.middleware("http")
async def log_requests(request: Request, call_next):
    # Generate unique session ID (8-char UUID)
    session_id = str(uuid.uuid4())[:8]
    set_session_id(session_id)

    # Log incoming request (method, path, query, body)
    logger.info(f"Request: {request.method} {request.url.path} | ...")

    # Process request and measure time
    start_time = time.time()
    response = await call_next(request)
    process_time = time.time() - start_time

    # Log response (status, time)
    logger.info(f"Response: {response.status_code} | Process Time: {process_time:.3f}s")

    # Clean up session ID
    clear_session_id()
```

**What Gets Logged**:
- Request method, path, query parameters, body (truncated to 200 chars)
- Response status code
- Processing time in seconds
- Exceptions with full stack trace

### 3. Updated `src/router.py`

**Changes**:
- Replaced `logging.getLogger(__name__)` with shared logger
- Imported `from src.logger import logger`

**Result**: All router logs now use consistent formatting and include session IDs

### 4. Updated `src/consumer.py`

**Changes**:
- Imported logger and session management functions
- Enhanced `consume_messages()` method with comprehensive logging

**Message Reception Logging**:
```python
logger.info(
    f"Received: Queue={self.input_queue_name} | "
    f"ConversationId={prompt_message.conversation_id} | "
    f"UserId={prompt_message.user_id} | "
    f"TenantId={prompt_message.tenant_id} | "
    f"Message={prompt_message.message[:100]}"
)
```

**Processing Result Logging**:

**Success**:
```python
logger.info(
    f"Success: ConversationId={prompt_message.conversation_id} | "
    f"Status=Processed"
)
```

**Error**:
```python
logger.error(
    f"Error: Failed to process message | "
    f"ConversationId={prompt_message.conversation_id} | "
    f"Reason={type(e).__name__} | "
    f"Details={str(e)}",
    exc_info=True
)
```

**What Gets Logged**:
- Message reception: Queue name, ConversationId, UserId, TenantId, Message preview
- Processing success: ConversationId, Status
- Processing errors: ConversationId, Error type, Details, Stack trace
- Each message gets unique session ID

### 5. Updated `src/business.py`

**Changes**:
- Replaced `logging.getLogger(__name__)` with shared logger
- All business logic now uses consistent logging format

## Log File Structure

```
Services/ChatProcessor/
├── logs/
│   ├── chatprocessor.log              # Current day's log
│   ├── chatprocessor.log.2025-12-16   # Previous day's log
│   ├── chatprocessor.log.2025-12-15   # 2 days ago
│   └── ...                             # Up to 30 days retained
├── src/
│   ├── logger.py                       # New logger module
│   ├── router.py                       # Updated with new logger
│   ├── consumer.py                     # Updated with logging
│   ├── business.py                     # Updated with new logger
│   └── ...
├── main.py                             # Updated with middleware
└── .gitignore                          # Already excludes logs/
```

## Session ID Tracking

### How It Works

1. **HTTP Requests (FastAPI)**:
   - Middleware generates 8-char session ID
   - Set at request start
   - All logs within request include same session ID
   - Cleared at request end

2. **RabbitMQ Messages**:
   - Consumer generates 8-char session ID per message
   - Set when message received
   - All logs during processing include same session ID
   - Cleared after processing (success or error)

### Benefits

- Trace all logs for a single request/message
- Easy debugging and troubleshooting
- Correlate logs across services

## Log Levels

| Level | Usage |
|-------|-------|
| **INFO** | Request/response logging, message processing, health checks |
| **ERROR** | Exceptions, failures, processing errors |
| **WARNING** | Degraded service states (existing usage) |

## Example Log Outputs

### HTTP Request Log

```
2025-12-17 10:30:45.123 [INF] a1b2c3d4 Request: POST /api/chat/test | Query: None | Body: {"conversation_id":1,"message":"What is labor law?","user_id":123,"tenant_id":2}
2025-12-17 10:30:45.234 [INF] a1b2c3d4 [ConversationId: 1] Processing message from User 123, Tenant 2: 'What is labor law?...'
2025-12-17 10:30:45.345 [INF] a1b2c3d4 Qdrant search completed: tenant_id=2, results=3
2025-12-17 10:30:50.456 [INF] a1b2c3d4 [ConversationId: 1] Generated response (length: 256)
2025-12-17 10:30:50.567 [INF] a1b2c3d4 Response: 200 | Process Time: 5.444s
```

### RabbitMQ Message Log

```
2025-12-17 10:31:00.123 [INF] e5f6g7h8 Received: Queue=UserPromptReceived | ConversationId=2 | UserId=456 | TenantId=3 | Message=How to create labor contract?
2025-12-17 10:31:00.234 [INF] e5f6g7h8 [ConversationId: 2] Processing message from User 456, Tenant 3: 'How to create labor contract?...'
2025-12-17 10:31:00.345 [INF] e5f6g7h8 Qdrant search completed: tenant_id=3, results=5
2025-12-17 10:31:05.456 [INF] e5f6g7h8 [ConversationId: 2] Generated response (length: 512)
2025-12-17 10:31:05.567 [INF] e5f6g7h8 Published response - ConversationId: 2
2025-12-17 10:31:05.678 [INF] e5f6g7h8 Success: ConversationId=2 | Status=Processed
```

### Error Log

```
2025-12-17 10:32:00.123 [INF] i9j0k1l2 Received: Queue=UserPromptReceived | ConversationId=3 | UserId=789 | TenantId=4 | Message=What is minimum wage?
2025-12-17 10:32:00.234 [ERR] i9j0k1l2 Error: Failed to process message | ConversationId=3 | Reason=HTTPStatusError | Details=Ollama error: 404
Traceback (most recent call last):
  File "src/consumer.py", line 87, in on_message
    await message_handler(prompt_message)
  ...
httpx.HTTPStatusError: Client error '404 Not Found' for url 'http://localhost:11434/api/chat'
```

## Configuration

No additional configuration needed. Logging is automatically enabled when the service starts.

**Configurable via code** (in `src/logger.py`):
- Log level: `logger.setLevel(logging.INFO)` (line 29)
- Rotation: `when='midnight'` (line 38)
- Retention: `backupCount=30` (line 40)
- Log directory: `logs/` (line 24)

## Testing

### Test HTTP Logging

```bash
curl -X POST "http://localhost:8001/api/chat/test" \
  -H "Content-Type: application/json" \
  -d '{
    "conversation_id": 1,
    "message": "Test message",
    "user_id": 123,
    "tenant_id": 2
  }'
```

**Check logs**:
```bash
tail -f Services/ChatProcessor/logs/chatprocessor.log
```

### Test RabbitMQ Logging

1. Start ChatProcessor service
2. Publish message to `UserPromptReceived` queue
3. Check logs for session ID, message reception, and processing result

## Benefits

1. **Consistent Format**: All logs follow same pattern as EmbeddingService
2. **Session Tracking**: Trace complete request/message lifecycle
3. **Daily Rotation**: Automatic log file management
4. **Retention Policy**: 30 days of logs kept automatically
5. **Dual Output**: Logs written to both file and console
6. **Comprehensive Coverage**: Logs at all integration points (API, RabbitMQ)
7. **Error Context**: Full stack traces with session correlation
8. **Performance Metrics**: Processing time logged for every request

## Troubleshooting

### Logs Not Appearing

1. Check logs directory exists: `Services/ChatProcessor/logs/`
2. Check file permissions for writing
3. Verify service is using new logger (not old basicConfig)

### Session IDs Not Showing

- Ensure `set_session_id()` called at request/message start
- Verify `SessionFormatter` is being used
- Check `session_context` is properly initialized

### Large Log Files

- Current retention: 30 days
- Daily rotation ensures manageable file sizes
- Adjust `backupCount` in `logger.py` if needed

---

**Implementation Date**: 2025-12-17
**Implemented By**: Claude Sonnet 4.5
**Version**: 1.0.0
