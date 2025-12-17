import json
from typing import Callable, Awaitable, Optional
import aio_pika
from aio_pika import Message, DeliveryMode
from aio_pika.abc import AbstractChannel, AbstractConnection, AbstractIncomingMessage

from src.config import settings
from src.schemas import UserPromptReceivedMessage, BotResponseCreatedMessage
from src.logger import logger, set_session_id, clear_session_id, get_session_id

class RabbitMQService:
    def __init__(self, host: Optional[str] = None, port: Optional[int] = None, username: Optional[str] = None, password: Optional[str] = None, input_queue: Optional[str] = None, output_queue: Optional[str] = None, prefetch_count: Optional[int] = None):
        self.host = host or settings.rabbitmq_host
        self.port = port or settings.rabbitmq_port
        self.username = username or settings.rabbitmq_username
        self.password = password or settings.rabbitmq_password
        self.input_queue_name = input_queue or settings.rabbitmq_queue_input
        self.output_queue_name = output_queue or settings.rabbitmq_queue_output
        self.prefetch_count = prefetch_count or settings.prefetch_count
        self.connection: Optional[AbstractConnection] = None
        self.channel: Optional[AbstractChannel] = None
        logger.info(f"Initialized RabbitMQService: {self.username}@{self.host}:{self.port}")

    async def connect(self) -> None:
        try:
            connection_url = f"amqp://{self.username}:{self.password}@{self.host}:{self.port}/"
            logger.info(f"Connecting to RabbitMQ at {self.host}:{self.port}")

            self.connection = await aio_pika.connect_robust(connection_url)
            self.channel = await self.connection.channel()
            await self.channel.set_qos(prefetch_count=self.prefetch_count)

            # Consumer: Bind to UserPromptReceived exchange (published by .NET)
            input_exchange = await self.channel.declare_exchange(name=self.input_queue_name, type=aio_pika.ExchangeType.FANOUT, durable=True)
            input_queue = await self.channel.declare_queue(name=self.input_queue_name, durable=True)
            await input_queue.bind(exchange=input_exchange, routing_key="")

            # Publisher: Declare output queue for direct publishing
            output_queue = await self.channel.declare_queue(name=self.output_queue_name, durable=True)

            logger.info("RabbitMQ topology configured")
        except Exception as e:
            logger.error(f"Failed to connect to RabbitMQ: {e}", exc_info=True)
            raise

    async def disconnect(self) -> None:
        try:
            if self.channel and not self.channel.is_closed:
                await self.channel.close()
                logger.info("RabbitMQ channel closed")
            if self.connection and not self.connection.is_closed:
                await self.connection.close()
                logger.info("RabbitMQ connection closed")
        except Exception as e:
            logger.error(f"Error during disconnect: {e}", exc_info=True)

    async def consume_messages(self, message_handler: Callable[[UserPromptReceivedMessage], Awaitable[None]]) -> None:
        if not self.channel:
            raise RuntimeError("Channel not initialized")

        try:
            queue = await self.channel.declare_queue(self.input_queue_name, durable=True)
            logger.info(f"Starting consumer on '{self.input_queue_name}'")

            async def on_message(message: AbstractIncomingMessage) -> None:
                async with message.process():
                    # Generate session ID for this message
                    import uuid
                    import traceback
                    session_id = str(uuid.uuid4())[:8]
                    set_session_id(session_id)

                    try:
                        # RAW LOG: Log raw message before parsing
                        body = message.body.decode()
                        logger.info(f"RAW: {body}")

                        # Parse JSON
                        data = json.loads(body)

                        # ENVELOPE HANDLING: Check if MassTransit wrapped the payload
                        if "message" in data:
                            logger.info("Detected MassTransit envelope, extracting payload")
                            payload = data["message"]
                        else:
                            logger.info("No envelope detected, using raw data as payload")
                            payload = data

                        # Parse the actual message
                        prompt_message = UserPromptReceivedMessage(**payload)

                        # Log message reception with details
                        logger.info(
                            f"Received: Queue={self.input_queue_name} | "
                            f"ConversationId={prompt_message.conversation_id} | "
                            f"UserId={prompt_message.user_id} | "
                            f"TenantId={prompt_message.tenant_id} | "
                            f"Message={prompt_message.message[:100]}"  # Limit to 100 chars
                        )

                        # Process the message
                        await message_handler(prompt_message)

                        # Log successful processing
                        logger.info(
                            f"Success: ConversationId={prompt_message.conversation_id} | "
                            f"Status=Processed"
                        )

                    except json.JSONDecodeError as e:
                        logger.error(
                            f"Error: Failed to parse message | "
                            f"Reason=JSONDecodeError | "
                            f"Details={str(e)}"
                        )
                        logger.error(f"Full traceback:\n{traceback.format_exc()}")
                    except Exception as e:
                        logger.error(
                            f"Error: Failed to process message | "
                            f"ConversationId={prompt_message.conversation_id if 'prompt_message' in locals() else 'Unknown'} | "
                            f"Reason={type(e).__name__} | "
                            f"Details={str(e)}"
                        )
                        logger.error(f"Full traceback:\n{traceback.format_exc()}")
                    finally:
                        # Message is auto-acknowledged by message.process() context manager
                        # even if errors occur, preventing queue blocking
                        clear_session_id()

            await queue.consume(on_message)
            logger.info(f"Consumer registered for '{self.input_queue_name}'")
        except Exception as e:
            logger.error(f"Failed to setup consumer: {e}", exc_info=True)
            raise

    async def publish_response(self, response: BotResponseCreatedMessage) -> None:
        if not self.channel:
            raise RuntimeError("Channel not initialized")

        try:
            import uuid
            from datetime import datetime, timezone

            # Serialize the payload using camelCase aliases
            payload = response.model_dump(by_alias=True, mode='json')

            # Wrap in MassTransit envelope format
            envelope = {
                "messageId": str(uuid.uuid4()),
                "conversationId": None,
                "sourceAddress": f"rabbitmq://localhost/{self.input_queue_name}",
                "destinationAddress": f"rabbitmq://localhost/{self.output_queue_name}",
                "messageType": [
                    "urn:message:ChatService.Events:BotResponseCreatedEvent"
                ],
                "message": payload,
                "sentTime": datetime.now(timezone.utc).isoformat(),
                "headers": {},
                "host": {
                    "machineName": "ChatProcessor",
                    "processName": "python",
                    "assembly": "ChatProcessor",
                    "assemblyVersion": "1.0.0"
                }
            }

            message_body = json.dumps(envelope)
            logger.info(f"Publishing MassTransit envelope to queue '{self.output_queue_name}': {message_body}")
            message = Message(body=message_body.encode(), delivery_mode=DeliveryMode.PERSISTENT, content_type="application/json")
            # Publish directly to BotResponseCreated queue using default exchange (built-in property)
            await self.channel.default_exchange.publish(message, routing_key=self.output_queue_name)
            logger.info(f"Published response - ConversationId: {response.conversation_id}")
        except Exception as e:
            logger.error(f"Failed to publish response: {e}", exc_info=True)
            raise

    async def health_check(self) -> bool:
        try:
            if self.connection and not self.connection.is_closed:
                if self.channel and not self.channel.is_closed:
                    logger.info("RabbitMQ health check passed")
                    return True
            logger.warning("RabbitMQ connection closed")
            return False
        except Exception as e:
            logger.error(f"RabbitMQ health check error: {e}")
            return False
