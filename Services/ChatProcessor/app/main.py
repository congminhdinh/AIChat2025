import asyncio
import logging
import signal
import sys
import platform
from typing import Optional
from datetime import datetime
import uvicorn
from app.config import settings
from app.models import UserPromptReceivedMessage, BotResponseCreatedMessage
from app.services import OllamaService, RabbitMQService
from app.services.qdrant_service import QdrantService
from app.services.service import process_chat_message
logging.basicConfig(level=getattr(logging, settings.log_level.upper()), format='%(asctime)s - %(name)s - %(levelname)s - %(message)s', handlers=[logging.StreamHandler(sys.stdout), logging.FileHandler('chatprocessor.log')])
logger = logging.getLogger(__name__)

class ChatProcessor:

    def __init__(self):
        self.rabbitmq_service = RabbitMQService()
        self.ollama_service = OllamaService()
        self.qdrant_service = QdrantService()
        self.shutdown_event = asyncio.Event()
        logger.info('ChatProcessor initialized')

    async def process_prompt(self, prompt_message: UserPromptReceivedMessage) -> None:
        try:
            result = await process_chat_message(conversation_id=prompt_message.conversation_id, user_id=prompt_message.user_id, message=prompt_message.message, tenant_id=prompt_message.tenant_id, ollama_service=self.ollama_service, qdrant_service=self.qdrant_service)
            response_message = BotResponseCreatedMessage(conversation_id=result['conversation_id'], message=result['message'], user_id=result['user_id'], tenant_id=result['tenant_id'], timestamp=result['timestamp'], model_used=result['model_used'])
            await self.rabbitmq_service.publish_response(response_message)
            logger.info(f'[ConversationId: {prompt_message.conversation_id}] Successfully published response')
        except Exception as e:
            logger.error(f'[ConversationId: {prompt_message.conversation_id}] Failed to process prompt: {e}', exc_info=True)

    async def run_rabbitmq(self) -> None:
        try:
            logger.info('Starting ChatProcessor Service')
            logger.info(f'Ollama URL: {settings.ollama_base_url}')
            logger.info(f'Ollama Model: {settings.ollama_model}')
            logger.info(f'RabbitMQ Host: {settings.rabbitmq_host}:{settings.rabbitmq_port}')
            logger.info(f'Qdrant Host: {settings.qdrant_host}:{settings.qdrant_port}')
            await self.rabbitmq_service.connect()
            logger.info('Performing health checks...')
            ollama_healthy = await self.ollama_service.health_check()
            qdrant_healthy = self.qdrant_service.health_check()
            rabbitmq_healthy = await self.rabbitmq_service.health_check()
            if not ollama_healthy:
                logger.warning('Ollama health check failed')
            if not qdrant_healthy:
                logger.warning('Qdrant health check failed')
            if not rabbitmq_healthy:
                raise RuntimeError('RabbitMQ health check failed')
            logger.info('Starting message consumer...')
            await self.rabbitmq_service.consume_messages(self.process_prompt)
            await self.shutdown_event.wait()
        except Exception as e:
            logger.error(f'Fatal error in ChatProcessor: {e}', exc_info=True)
            raise
        finally:
            logger.info('Shutting down ChatProcessor...')
            await self.rabbitmq_service.disconnect()

    async def run_fastapi(self) -> None:
        config = uvicorn.Config('app.api:app', host=settings.fastapi_host, port=settings.fastapi_port, log_level=settings.log_level.lower())
        server = uvicorn.Server(config)
        await server.serve()

    def handle_shutdown(self, signum: Optional[int]=None, frame: Optional[object]=None) -> None:
        logger.info(f'Received shutdown signal ({signum})')
        self.shutdown_event.set()

async def main() -> None:
    processor = ChatProcessor()
    if platform.system() == 'Windows':

        def signal_handler(signum, frame):
            logger.info(f'Received signal {signum}')
            processor.shutdown_event.set()
        signal.signal(signal.SIGINT, signal_handler)
        signal.signal(signal.SIGTERM, signal_handler)
    else:
        loop = asyncio.get_running_loop()
        for sig in (signal.SIGTERM, signal.SIGINT):
            loop.add_signal_handler(sig, processor.handle_shutdown, sig, None)
    try:
        rabbitmq_task = asyncio.create_task(processor.run_rabbitmq())
        fastapi_task = asyncio.create_task(processor.run_fastapi())
        logger.info('ChatProcessor running with RabbitMQ consumer and FastAPI endpoint')
        logger.info(f'FastAPI available at http://{settings.fastapi_host}:{settings.fastapi_port}')
        await asyncio.gather(rabbitmq_task, fastapi_task)
    except KeyboardInterrupt:
        logger.info('Received keyboard interrupt')
        processor.handle_shutdown()
    except Exception as e:
        logger.error(f'Unhandled exception: {e}', exc_info=True)
        sys.exit(1)
if __name__ == '__main__':
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        logger.info('Service stopped by user')
    except Exception as e:
        logger.error(f'Service crashed: {e}', exc_info=True)
        sys.exit(1)