"""RabbitMQ service for consuming and publishing messages."""

import json
import logging
from typing import Callable, Awaitable, Optional
import aio_pika
from aio_pika import Message, DeliveryMode
from aio_pika.abc import AbstractChannel, AbstractConnection, AbstractIncomingMessage
from app.config import settings
from app.models import UserPromptReceivedMessage, BotResponseCreatedMessage

logger = logging.getLogger(__name__)


class RabbitMQService:
    """
    Service for RabbitMQ message consumption and publishing.

    Handles connection to RabbitMQ, consuming messages from the input queue,
    and publishing responses to the output queue.
    """

    def __init__(
        self,
        host: Optional[str] = None,
        port: Optional[int] = None,
        username: Optional[str] = None,
        password: Optional[str] = None,
        input_queue: Optional[str] = None,
        output_queue: Optional[str] = None,
        prefetch_count: Optional[int] = None
    ):
        """
        Initialize RabbitMQ service.

        Args:
            host: RabbitMQ host (defaults to settings.rabbitmq_host)
            port: RabbitMQ port (defaults to settings.rabbitmq_port)
            username: RabbitMQ username (defaults to settings.rabbitmq_username)
            password: RabbitMQ password (defaults to settings.rabbitmq_password)
            input_queue: Input queue name (defaults to settings.rabbitmq_queue_input)
            output_queue: Output queue name (defaults to settings.rabbitmq_queue_output)
            prefetch_count: Number of messages to prefetch (defaults to settings.prefetch_count)
        """
        self.host = host or settings.rabbitmq_host
        self.port = port or settings.rabbitmq_port
        self.username = username or settings.rabbitmq_username
        self.password = password or settings.rabbitmq_password
        self.input_queue_name = input_queue or settings.rabbitmq_queue_input
        self.output_queue_name = output_queue or settings.rabbitmq_queue_output
        self.prefetch_count = prefetch_count or settings.prefetch_count

        self.connection: Optional[AbstractConnection] = None
        self.channel: Optional[AbstractChannel] = None

        logger.info(
            f"Initialized RabbitMQService: {self.username}@{self.host}:{self.port}, "
            f"input_queue={self.input_queue_name}, output_queue={self.output_queue_name}"
        )

    async def connect(self) -> None:
        """
        Establish connection to RabbitMQ and configure exchanges, queues, and bindings.

        This method ensures that:
        1. Exchanges are declared (matching MassTransit's naming convention)
        2. Queues are declared
        3. Queues are BOUND to exchanges (critical for message flow)

        Raises:
            Exception: If connection fails
        """
        try:
            connection_url = f"amqp://{self.username}:{self.password}@{self.host}:{self.port}/"
            logger.info(f"Connecting to RabbitMQ at {self.host}:{self.port}...")

            self.connection = await aio_pika.connect_robust(connection_url)
            self.channel = await self.connection.channel()
            await self.channel.set_qos(prefetch_count=self.prefetch_count)

            # ═══ DECLARE EXCHANGES ═══
            # MassTransit creates fanout exchanges named after the message type
            # Exchange names match the C# event class names (without "Event" suffix)

            # Input Exchange: UserPromptReceived (published by ChatService)
            input_exchange = await self.channel.declare_exchange(
                name=self.input_queue_name,  # "UserPromptReceived"
                type=aio_pika.ExchangeType.FANOUT,
                durable=True
            )
            logger.info(f"✅ Declared exchange: {self.input_queue_name} (fanout)")

            # Output Exchange: BotResponseCreated (consumed by ChatService)
            output_exchange = await self.channel.declare_exchange(
                name=self.output_queue_name,  # "BotResponseCreated"
                type=aio_pika.ExchangeType.FANOUT,
                durable=True
            )
            logger.info(f"✅ Declared exchange: {self.output_queue_name} (fanout)")

            # ═══ DECLARE QUEUES ═══
            input_queue = await self.channel.declare_queue(
                name=self.input_queue_name,
                durable=True
            )
            logger.info(f"✅ Declared queue: {self.input_queue_name}")

            output_queue = await self.channel.declare_queue(
                name=self.output_queue_name,
                durable=True
            )
            logger.info(f"✅ Declared queue: {self.output_queue_name}")

            # ═══ BIND QUEUES TO EXCHANGES ═══
            # THIS IS THE CRITICAL STEP that was missing!
            # Without binding, messages published to the exchange won't reach the queue

            await input_queue.bind(
                exchange=input_exchange,
                routing_key=""  # Fanout exchanges ignore routing keys
            )
            logger.info(f"✅ Bound queue '{self.input_queue_name}' to exchange '{self.input_queue_name}'")

            await output_queue.bind(
                exchange=output_exchange,
                routing_key=""  # Fanout exchanges ignore routing keys
            )
            logger.info(f"✅ Bound queue '{self.output_queue_name}' to exchange '{self.output_queue_name}'")

            print("\n" + "=" * 80)
            print("🔗 RABBITMQ TOPOLOGY CONFIGURED")
            print("=" * 80)
            print(f"  Input:  Exchange '{self.input_queue_name}' → Queue '{self.input_queue_name}'")
            print(f"  Output: Exchange '{self.output_queue_name}' → Queue '{self.output_queue_name}'")
            print("=" * 80 + "\n")

            logger.info("Successfully connected to RabbitMQ and configured topology")

        except Exception as e:
            logger.error(f"Failed to connect to RabbitMQ: {e}", exc_info=True)
            raise

    async def disconnect(self) -> None:
        """Close RabbitMQ connection gracefully."""
        try:
            if self.channel and not self.channel.is_closed:
                await self.channel.close()
                logger.info("RabbitMQ channel closed")

            if self.connection and not self.connection.is_closed:
                await self.connection.close()
                logger.info("RabbitMQ connection closed")

        except Exception as e:
            logger.error(f"Error during RabbitMQ disconnect: {e}", exc_info=True)

    async def consume_messages(
        self,
        message_handler: Callable[[UserPromptReceivedMessage], Awaitable[None]]
    ) -> None:
        """
        Start consuming messages from the input queue.

        Args:
            message_handler: Async callback function to process each message
                           Takes UserPromptReceivedMessage as input

        Raises:
            Exception: If consumption setup fails
        """
        if not self.channel:
            raise RuntimeError("Channel not initialized. Call connect() first.")

        try:
            queue = await self.channel.declare_queue(self.input_queue_name, durable=True)

            logger.info(f"Starting to consume messages from '{self.input_queue_name}' queue...")

            async def on_message(message: AbstractIncomingMessage) -> None:
                """Internal callback for processing incoming messages."""
                async with message.process():
                    try:
                        # Parse message body
                        body = message.body.decode()
                        logger.debug(f"Received raw message: {body}")

                        # Deserialize to Pydantic model
                        data = json.loads(body)
                        prompt_message = UserPromptReceivedMessage(**data)

                        # ═══ CONSOLE LOG: MESSAGE RECEIVED ═══
                        print("\n" + "═" * 80)
                        print("📨 NEW MESSAGE RECEIVED FROM RABBITMQ")
                        print("═" * 80)
                        print(f"  Conversation ID: {prompt_message.conversation_id}")
                        print(f"  User ID:         {prompt_message.user_id}")
                        print(f"  Timestamp:       {prompt_message.timestamp}")
                        print(f"  Message:         {prompt_message.message[:100]}{'...' if len(prompt_message.message) > 100 else ''}")
                        print("═" * 80 + "\n")

                        logger.info(
                            f"Processing message - ConversationId: {prompt_message.conversation_id}, "
                            f"UserId: {prompt_message.user_id}"
                        )

                        # Call the user-provided handler
                        await message_handler(prompt_message)

                        logger.info(
                            f"Successfully processed message for ConversationId: {prompt_message.conversation_id}"
                        )

                    except json.JSONDecodeError as e:
                        logger.error(f"Failed to parse message JSON: {e}", exc_info=True)
                        # Message will be rejected and not requeued

                    except Exception as e:
                        logger.error(f"Error processing message: {e}", exc_info=True)
                        # Message will be rejected and not requeued
                        # In production, consider implementing dead letter queue

            await queue.consume(on_message)
            logger.info(f"Consumer registered for queue '{self.input_queue_name}'")

        except Exception as e:
            logger.error(f"Failed to set up message consumer: {e}", exc_info=True)
            raise

    async def publish_response(self, response: BotResponseCreatedMessage) -> None:
        """
        Publish AI response to the output exchange.

        Publishes to the BotResponseCreated exchange (fanout), which is then
        consumed by the .NET ChatService via the bound queue.

        Args:
            response: BotResponseCreatedMessage to publish

        Raises:
            Exception: If publishing fails
        """
        if not self.channel:
            raise RuntimeError("Channel not initialized. Call connect() first.")

        try:
            # Serialize to JSON
            message_body = response.model_dump_json()

            logger.debug(f"Publishing response: {message_body}")

            message = Message(
                body=message_body.encode(),
                delivery_mode=DeliveryMode.PERSISTENT,
                content_type="application/json"
            )

            # Get the output exchange (BotResponseCreated)
            output_exchange = await self.channel.get_exchange(self.output_queue_name)

            # Publish to the exchange (fanout exchanges ignore routing keys)
            await output_exchange.publish(
                message,
                routing_key=""  # Fanout exchange - routing key is ignored
            )

            logger.info(
                f"Published response to '{self.output_queue_name}' - "
                f"ConversationId: {response.conversation_id}"
            )

        except Exception as e:
            logger.error(f"Failed to publish response: {e}", exc_info=True)
            raise

    async def health_check(self) -> bool:
        """
        Check if RabbitMQ connection is healthy.

        Returns:
            True if connected and healthy, False otherwise
        """
        try:
            if self.connection and not self.connection.is_closed:
                if self.channel and not self.channel.is_closed:
                    logger.info("RabbitMQ health check passed")
                    return True
            logger.warning("RabbitMQ health check failed: connection or channel closed")
            return False
        except Exception as e:
            logger.error(f"RabbitMQ health check error: {e}")
            return False
