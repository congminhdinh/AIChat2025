"""
ChatProcessor - AI Worker Service

Main entry point for the ChatProcessor service.
Listens to RabbitMQ for user prompts and generates AI responses using Ollama.
"""

import asyncio
import logging
import signal
import sys
import platform
from typing import Optional
from datetime import datetime

from app.config import settings
from app.models import UserPromptReceivedMessage, BotResponseCreatedMessage
from app.services import OllamaService, RabbitMQService

# Configure logging
logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler("chatprocessor.log")
    ]
)

logger = logging.getLogger(__name__)


class ChatProcessor:
    """
    Main ChatProcessor service orchestrator.

    Coordinates between RabbitMQ and Ollama services to process user prompts
    and generate AI responses.
    """

    def __init__(self):
        """Initialize ChatProcessor with required services."""
        self.rabbitmq_service = RabbitMQService()
        self.ollama_service = OllamaService()
        self.shutdown_event = asyncio.Event()

        logger.info("ChatProcessor initialized")

    async def process_prompt(self, prompt_message: UserPromptReceivedMessage) -> None:
        """
        Process a user prompt and publish the AI response.

        Args:
            prompt_message: Incoming user prompt message

        This is the core processing logic:
        1. Receive prompt from RabbitMQ
        2. Call Ollama API to generate response
        3. Publish response back to RabbitMQ with original conversation_id
        """
        try:
            conversation_id = prompt_message.conversation_id
            user_prompt = prompt_message.message
            user_id = prompt_message.user_id

            logger.info(
                f"[ConversationId: {conversation_id}] Processing prompt from User {user_id}: "
                f"'{user_prompt[:50]}...'"
            )

            # ═══ CONSOLE LOG: QUERYING CONTEXT ═══
            print("\n" + "─" * 80)
            print("🔍 QUERYING CONVERSATION CONTEXT")
            print("─" * 80)
            print(f"  Conversation ID: {conversation_id}")
            print(f"  Context Status:  No history (TODO: implement context retrieval)")
            print(f"  Note:            Future enhancement will retrieve message history")
            print("─" * 80 + "\n")

            # Generate AI response using Ollama
            ai_response = await self.ollama_service.generate_response(
                prompt=user_prompt,
                conversation_history=None  # TODO: Add conversation history support
            )

            logger.info(
                f"[ConversationId: {conversation_id}] Generated response (length: {len(ai_response)})"
            )

            # Create response message with CRITICAL conversation_id
            response_message = BotResponseCreatedMessage(
                conversation_id=conversation_id,  # REQUIRED for .NET service routing
                message=ai_response,
                user_id=0,  # System/Bot user ID
                timestamp=datetime.utcnow(),
                model_used=self.ollama_service.model
            )

            # Publish response to output queue
            await self.rabbitmq_service.publish_response(response_message)

            # ═══ CONSOLE LOG: PROCESSING COMPLETE ═══
            print("\n" + "█" * 80)
            print("✨ MESSAGE PROCESSING COMPLETE")
            print("█" * 80)
            print(f"  Conversation ID: {conversation_id}")
            print(f"  Response sent to: {settings.rabbitmq_queue_output}")
            print(f"  Model used:       {self.ollama_service.model}")
            print(f"  Total time:       Processing complete")
            print("█" * 80 + "\n")

            logger.info(
                f"[ConversationId: {conversation_id}] Successfully published response to "
                f"'{settings.rabbitmq_queue_output}' queue"
            )

        except Exception as e:
            logger.error(
                f"[ConversationId: {prompt_message.conversation_id}] Failed to process prompt: {e}",
                exc_info=True
            )
            # In production, consider publishing error message or retry logic

    async def run(self) -> None:
        """
        Main run loop for the ChatProcessor service.

        Handles:
        1. Service initialization
        2. Health checks
        3. Message consumption
        4. Graceful shutdown
        """
        try:
            logger.info("=" * 60)
            logger.info("Starting ChatProcessor Service")
            logger.info("=" * 60)
            logger.info(f"Configuration:")
            logger.info(f"  - Ollama URL: {settings.ollama_base_url}")
            logger.info(f"  - Ollama Model: {settings.ollama_model}")
            logger.info(f"  - RabbitMQ Host: {settings.rabbitmq_host}:{settings.rabbitmq_port}")
            logger.info(f"  - Input Queue: {settings.rabbitmq_queue_input}")
            logger.info(f"  - Output Queue: {settings.rabbitmq_queue_output}")
            logger.info("=" * 60)

            # Connect to RabbitMQ
            await self.rabbitmq_service.connect()

            # Health check for Ollama
            logger.info("Performing health checks...")
            ollama_healthy = await self.ollama_service.health_check()
            rabbitmq_healthy = await self.rabbitmq_service.health_check()

            if not ollama_healthy:
                logger.warning("Ollama health check failed, but continuing anyway...")

            if not rabbitmq_healthy:
                raise RuntimeError("RabbitMQ health check failed")

            logger.info("All health checks passed")

            # Start consuming messages
            logger.info("Starting message consumer...")
            await self.rabbitmq_service.consume_messages(self.process_prompt)

            logger.info("ChatProcessor is now running. Press Ctrl+C to stop.")

            # Wait for shutdown signal
            await self.shutdown_event.wait()

        except Exception as e:
            logger.error(f"Fatal error in ChatProcessor: {e}", exc_info=True)
            raise

        finally:
            logger.info("Shutting down ChatProcessor...")
            await self.rabbitmq_service.disconnect()
            logger.info("ChatProcessor stopped")

    def handle_shutdown(self, signum: Optional[int] = None, frame: Optional[object] = None) -> None:
        """
        Handle shutdown signals gracefully.

        Args:
            signum: Signal number
            frame: Current stack frame
        """
        logger.info(f"Received shutdown signal ({signum}). Initiating graceful shutdown...")
        self.shutdown_event.set()


async def main() -> None:
    """Main entry point."""
    processor = ChatProcessor()

    # Register signal handlers for graceful shutdown
    # Windows doesn't support add_signal_handler, so we use signal.signal instead
    if platform.system() == "Windows":
        # On Windows, use signal.signal()
        def signal_handler(signum, frame):
            logger.info(f"Received signal {signum}")
            processor.shutdown_event.set()

        signal.signal(signal.SIGINT, signal_handler)
        signal.signal(signal.SIGTERM, signal_handler)
    else:
        # On Unix/Linux, use asyncio's add_signal_handler
        loop = asyncio.get_running_loop()
        for sig in (signal.SIGTERM, signal.SIGINT):
            loop.add_signal_handler(
                sig,
                processor.handle_shutdown,
                sig,
                None
            )

    try:
        await processor.run()
    except KeyboardInterrupt:
        logger.info("Received keyboard interrupt")
        processor.handle_shutdown()
    except Exception as e:
        logger.error(f"Unhandled exception: {e}", exc_info=True)
        sys.exit(1)


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        logger.info("Service stopped by user")
    except Exception as e:
        logger.error(f"Service crashed: {e}", exc_info=True)
        sys.exit(1)
