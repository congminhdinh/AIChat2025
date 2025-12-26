import logging
from typing import Optional, List, Dict, Any
import httpx
from app.config import settings
logger = logging.getLogger(__name__)

class OllamaService:

    def __init__(self, base_url: Optional[str]=None, model: Optional[str]=None, timeout: Optional[int]=None):
        self.base_url = (base_url or settings.ollama_base_url).rstrip('/')
        self.model = model or settings.ollama_model
        self.timeout = timeout or settings.ollama_timeout
        self.chat_endpoint = f'{self.base_url}/api/chat'
        logger.info(f'Initialized OllamaService: base_url={self.base_url}, model={self.model}, timeout={self.timeout}s')

    async def generate_response(self, prompt: str, conversation_history: Optional[List[Dict[str, str]]]=None, stream: bool=False, temperature: Optional[float]=None) -> str:
        messages = conversation_history or []
        messages.append({'role': 'user', 'content': prompt})
        payload = {'model': self.model, 'messages': messages, 'stream': stream}

        # Add temperature to reduce hallucination if provided
        if temperature is not None:
            payload['options'] = {'temperature': temperature}
            logger.debug(f'Setting temperature to {temperature} for reduced hallucination')

        logger.debug(f'Sending request to Ollama: {payload}')
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                response = await client.post(self.chat_endpoint, json=payload)
                response.raise_for_status()
                data = response.json()
                if 'message' in data and 'content' in data['message']:
                    ai_response = data['message']['content']
                    logger.info(f'Generated response (length: {len(ai_response)})')
                    return ai_response
                else:
                    logger.error(f'Unexpected response format: {data}')
                    raise ValueError(f'Unexpected response format: {data}')
        except httpx.TimeoutException as e:
            logger.error(f'Timeout calling Ollama: {e}')
            raise Exception(f'Ollama timeout after {self.timeout}s: {str(e)}')
        except httpx.HTTPStatusError as e:
            logger.error(f'HTTP error from Ollama: {e.response.status_code}')
            raise Exception(f'Ollama error: {e.response.status_code}')
        except httpx.RequestError as e:
            logger.error(f'Request error calling Ollama: {e}')
            raise Exception(f'Failed to connect to Ollama: {str(e)}')
        except Exception as e:
            logger.error(f'Error during AI generation: {e}', exc_info=True)
            raise

    async def health_check(self) -> bool:
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                response = await client.get(f'{self.base_url}/api/tags')
                response.raise_for_status()
                logger.info('Ollama health check passed')
                return True
        except Exception as e:
            logger.error(f'Ollama health check failed: {e}')
            return False