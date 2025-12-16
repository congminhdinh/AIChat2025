# RabbitMQ Setup & MassTransit Integration Guide

## 🔍 The Problem: Missing Exchange-Queue Binding

### What Was Happening

**Before the fix:**
```
ChatService (MassTransit) → Exchange "UserPromptReceived" ❌ Queue "UserPromptReceived" ← ChatProcessor
                                     (NO BINDING!)
```

- **ChatService** published messages to **Exchange** `UserPromptReceived`
- **ChatProcessor** was listening to **Queue** `UserPromptReceived`
- **Messages were lost** because the queue was NOT bound to the exchange

### Root Cause: MassTransit Naming Convention

**MassTransit automatically creates Fanout Exchanges** named after the message type:

```csharp
// C# Event Definition
public record UserPromptReceivedEvent { ... }

// MassTransit creates:
// ✅ Exchange Name: "UserPromptReceived" (message type without "Event" suffix)
// ✅ Exchange Type: Fanout
```

**The Python service was only creating a queue**, not binding it to the exchange that MassTransit created.

## ✅ The Solution: Declare Exchange + Queue + Binding

### Updated Python Code Flow

```python
# 1. Declare Exchange (matching MassTransit's name)
exchange = await channel.declare_exchange(
    name="UserPromptReceived",  # Same name MassTransit uses
    type=ExchangeType.FANOUT,
    durable=True
)

# 2. Declare Queue
queue = await channel.declare_queue(
    name="UserPromptReceived",
    durable=True
)

# 3. BIND Queue to Exchange (THIS WAS MISSING!)
await queue.bind(
    exchange=exchange,
    routing_key=""  # Fanout ignores routing keys
)
```

### Complete RabbitMQ Topology

**After the fix:**
```
┌─────────────────────────────────────────────────────────────┐
│ INPUT PATH (User → AI)                                      │
└─────────────────────────────────────────────────────────────┘

ChatService (.NET)
    │
    │ publish()
    ▼
Exchange: UserPromptReceived (fanout)
    │
    │ binding
    ▼
Queue: UserPromptReceived
    │
    │ consume()
    ▼
ChatProcessor (Python)


┌─────────────────────────────────────────────────────────────┐
│ OUTPUT PATH (AI → User)                                     │
└─────────────────────────────────────────────────────────────┘

ChatProcessor (Python)
    │
    │ publish()
    ▼
Exchange: BotResponseCreated (fanout)
    │
    │ binding
    ▼
Queue: BotResponseCreated
    │
    │ consume()
    ▼
ChatService (.NET)
```

## 📋 MassTransit Naming Convention Explained

### How MassTransit Names Exchanges

| C# Event Class | Exchange Name | Exchange Type |
|----------------|---------------|---------------|
| `UserPromptReceivedEvent` | `UserPromptReceived` | Fanout |
| `BotResponseCreatedEvent` | `BotResponseCreated` | Fanout |
| `OrderPlacedEvent` | `OrderPlaced` | Fanout |

**Rules:**
1. Takes the **message type name**
2. Removes the **"Event" suffix** (if present)
3. Creates a **Fanout exchange** with that name

### Why Fanout Exchange?

**Fanout Exchange** broadcasts messages to **all bound queues**, ignoring routing keys.

```
Exchange (Fanout)
    ├─→ Queue 1 (Python Service)
    ├─→ Queue 2 (Logging Service)
    └─→ Queue 3 (Analytics Service)
```

Perfect for **pub/sub** patterns where multiple consumers need the same message.

## 🔧 Code Changes Summary

### Python Service (`rabbitmq_service.py`)

**Changed:**
```python
async def connect(self):
    # OLD: Only declared queues
    await channel.declare_queue(self.input_queue_name, durable=True)

    # NEW: Declare exchange, queue, AND binding
    exchange = await channel.declare_exchange(
        name=self.input_queue_name,
        type=aio_pika.ExchangeType.FANOUT,
        durable=True
    )
    queue = await channel.declare_queue(
        name=self.input_queue_name,
        durable=True
    )
    await queue.bind(exchange=exchange, routing_key="")  # ← THE FIX!
```

**Also changed:**
```python
async def publish_response(self, response):
    # OLD: Published to default exchange (direct routing)
    await channel.default_exchange.publish(
        message,
        routing_key=self.output_queue_name
    )

    # NEW: Publish to named fanout exchange
    exchange = await channel.get_exchange(self.output_queue_name)
    await exchange.publish(message, routing_key="")
```

## 🧪 Verification Steps

### 1. Check RabbitMQ Management UI

After starting ChatProcessor, verify in `http://localhost:15672`:

**Exchanges Tab:**
- ✅ `UserPromptReceived` (type: fanout, durable)
- ✅ `BotResponseCreated` (type: fanout, durable)

**Queues Tab:**
- ✅ `UserPromptReceived` (durable, 1 binding)
- ✅ `BotResponseCreated` (durable, 1 binding)

**Click on a Queue → Bindings:**
- ✅ Should show binding to exchange with same name

### 2. Test Message Flow

```bash
# Send a message via ChatService REST API
curl -X POST http://localhost:5218/api/chat/messages \
  -H "Content-Type: application/json" \
  -d '{"conversationId": 1, "message": "Test"}'

# Check ChatProcessor logs - you should see:
# 📨 NEW MESSAGE RECEIVED FROM RABBITMQ
#   Conversation ID: 1
#   Message: Test
```

### 3. Console Output on Startup

When ChatProcessor starts, you'll see:

```
═════════════════════════════════════════════════════════════════════════════
🔗 RABBITMQ TOPOLOGY CONFIGURED
═════════════════════════════════════════════════════════════════════════════
  Input:  Exchange 'UserPromptReceived' → Queue 'UserPromptReceived'
  Output: Exchange 'BotResponseCreated' → Queue 'BotResponseCreated'
═════════════════════════════════════════════════════════════════════════════
```

## 🚨 Common Issues & Solutions

### Issue 1: "Queue exists with different parameters"

**Error:**
```
PreconditionFailed: inequivalent arg 'x-queue-type'
```

**Cause:** Queue was created with different settings previously.

**Solution:**
```bash
# Delete the queue in RabbitMQ Management UI, or:
rabbitmqctl delete_queue UserPromptReceived
rabbitmqctl delete_queue BotResponseCreated
```

### Issue 2: Messages still not flowing

**Check:**
1. Both services are running
2. Both are connected to same RabbitMQ instance
3. Exchange type is **fanout** (not direct/topic)
4. Bindings exist (check UI)

### Issue 3: "Exchange not found" when publishing

**Cause:** Exchange wasn't declared before publishing.

**Solution:** Ensure `connect()` is called before `publish_response()`.

## 📚 Additional Resources

- [MassTransit Documentation](https://masstransit.io/)
- [RabbitMQ Exchanges Tutorial](https://www.rabbitmq.com/tutorials/tutorial-four-python.html)
- [aio-pika Documentation](https://aio-pika.readthedocs.io/)

## 🎯 Key Takeaways

1. **MassTransit creates fanout exchanges** named after message types
2. **Python service must bind queues to these exchanges**
3. **Declare exchange + queue + binding** in correct order
4. **Fanout exchanges ignore routing keys** (use empty string)
5. **Test bindings in RabbitMQ Management UI** before running services
