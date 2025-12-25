import asyncio
import signal
import sys
import platform
import uvicorn
import time
import json
import uuid
import jwt
from typing import Optional, Dict
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from src.config import settings
from src.router import router
from src.schemas import UserPromptReceivedMessage, BotResponseCreatedMessage
from src.consumer import RabbitMQService
from src.business import OllamaService, QdrantService, ChatBusiness
from src.logger import logger, set_session_id, clear_session_id
app = FastAPI(title='ChatProcessor API', version='1.0.0')

@app.middleware('http')
async def log_requests(request: Request, call_next):
    session_id = str(uuid.uuid4())[:8]
    set_session_id(session_id)
    try:
        body = await request.body()
        body_str = body.decode('utf-8') if body else ''
        query_params = dict(request.query_params)
        logger.info(f"Request: {request.method} {request.url.path} | Query: {(json.dumps(query_params) if query_params else 'None')} | Body: {(body_str[:200] if body_str else 'None')}")
    except Exception as e:
        logger.error(f'Error logging request: {str(e)}')
    start_time = time.time()
    try:
        response = await call_next(request)
        process_time = time.time() - start_time
        logger.info(f'Response: {response.status_code} | Process Time: {process_time:.3f}s')
        return response
    except Exception as e:
        process_time = time.time() - start_time
        logger.error(f'Exception: {str(e)} | Process Time: {process_time:.3f}s', exc_info=True)
        clear_session_id()
        return JSONResponse(status_code=500, content={'detail': str(e)})
    finally:
        clear_session_id()
app.include_router(router)

class JWTValidator:
    """
    JWT token validator that replicates the C# validation logic.

    Algorithm: HS256 (HMAC SHA 256)
    Verify Signature: YES
    Verify Issuer (iss): NO
    Verify Audience (aud): NO
    Verify Expiration (exp): YES
    """

    @staticmethod
    def validate_token(token: str) -> Optional[Dict]:
        """
        Validates a JWT token and extracts claims.

        Args:
            token: The JWT token string to validate

        Returns:
            Dictionary containing extracted claims (user_id, tenant_id, username) if valid,
            None if invalid or expired
        """
        try:
            # Decode and validate the token
            # options parameter disables issuer and audience verification
            decoded = jwt.decode(
                token,
                settings.jwt_secret_key,
                algorithms=["HS256"],
                options={
                    "verify_signature": True,
                    "verify_exp": True,
                    "verify_iss": False,
                    "verify_aud": False
                }
            )

            # Extract claims
            # user_id can be in 'User' claim or NameIdentifier claim
            user_id = decoded.get('User')
            if user_id is None:
                # Try the NameIdentifier claim (full URI)
                user_id = decoded.get('http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier')

            tenant_id = decoded.get('Tenant')
            username = decoded.get('http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name')

            # Handle list values (some JWT libraries return claims as lists)
            if isinstance(user_id, list) and len(user_id) > 0:
                user_id = user_id[0]
            if isinstance(tenant_id, list) and len(tenant_id) > 0:
                tenant_id = tenant_id[0]
            if isinstance(username, list) and len(username) > 0:
                username = username[0]

            # Convert to integers if they're strings
            if user_id is not None:
                user_id = int(user_id)
            if tenant_id is not None:
                tenant_id = int(tenant_id)

            logger.info(f'Token validated successfully: user_id={user_id}, tenant_id={tenant_id}, username={username}')

            return {
                'user_id': user_id,
                'tenant_id': tenant_id,
                'username': username,
                'claims': decoded
            }

        except jwt.ExpiredSignatureError:
            logger.error('Token validation failed: Token has expired')
            return None
        except jwt.InvalidTokenError as e:
            logger.error(f'Token validation failed: Invalid token - {str(e)}')
            return None
        except Exception as e:
            logger.error(f'Token validation failed: Unexpected error - {str(e)}', exc_info=True)
            return None

class ChatProcessor:

    def __init__(self):
        self.rabbitmq_service = RabbitMQService()
        self.ollama_service = OllamaService()
        self.qdrant_service = QdrantService()
        self.shutdown_event = asyncio.Event()
        logger.info('ChatProcessor initialized')

    async def process_prompt(self, prompt_message: UserPromptReceivedMessage) -> None:
        try:
            # Step 1: Validate JWT token
            logger.info(f'[ConversationId: {prompt_message.conversation_id}] Validating JWT token')
            token_claims = JWTValidator.validate_token(prompt_message.token)

            # Step 2: If token is invalid or expired, log error and do not process
            if token_claims is None:
                logger.error(f'[ConversationId: {prompt_message.conversation_id}] Token validation failed - Message will not be processed')
                # Do not send error response to user, just skip processing
                return

            # Step 3: Extract user_id and tenant_id from token claims
            user_id = token_claims.get('user_id')
            tenant_id = token_claims.get('tenant_id')

            if user_id is None or tenant_id is None:
                logger.error(f'[ConversationId: {prompt_message.conversation_id}] Missing user_id or tenant_id in token claims')
                return

            logger.info(f'[ConversationId: {prompt_message.conversation_id}] Token valid - Proceeding with message processing')

            # Step 4: Process the message with RAG logic
            system_instruction = None
            if prompt_message.system_instruction:
                system_instruction = [{'key': item.key, 'value': item.value} for item in prompt_message.system_instruction]

            result = await ChatBusiness.process_chat_message(
                conversation_id=prompt_message.conversation_id,
                user_id=user_id,
                message=prompt_message.message,
                tenant_id=tenant_id,
                ollama_service=self.ollama_service,
                qdrant_service=self.qdrant_service,
                system_instruction=system_instruction
            )

            # Step 5: Construct response with the original token
            response_message = BotResponseCreatedMessage(
                conversation_id=result['conversation_id'],
                message=result['message'],
                token=prompt_message.token,  # Include the exact token from input
                timestamp=result['timestamp'],
                model_used=result['model_used']
            )

            await self.rabbitmq_service.publish_response(response_message)
            logger.info(f'[ConversationId: {prompt_message.conversation_id}] Successfully published response')

        except Exception as e:
            logger.error(f'[ConversationId: {prompt_message.conversation_id}] Failed to process prompt: {e}', exc_info=True)
            try:
                from datetime import datetime
                # Send error response with the original token
                error_response = BotResponseCreatedMessage(
                    conversation_id=prompt_message.conversation_id,
                    message='Có lỗi xảy ra, vui lòng thử lại',
                    token=prompt_message.token,
                    timestamp=datetime.utcnow(),
                    model_used='error'
                )
                await self.rabbitmq_service.publish_response(error_response)
                logger.info(f'[ConversationId: {prompt_message.conversation_id}] Published error message to user')
            except Exception as publish_error:
                logger.error(f'[ConversationId: {prompt_message.conversation_id}] Failed to publish error message: {publish_error}', exc_info=True)

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
        config = uvicorn.Config('main:app', host=settings.fastapi_host, port=settings.fastapi_port, log_level=settings.log_level.lower())
        server = uvicorn.Server(config)
        await server.serve()

    def handle_shutdown(self, signum=None, frame=None) -> None:
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