"""Ollama API service for generating AI responses."""

import logging
from typing import Optional, List, Dict, Any
import httpx
from app.config import settings

logger = logging.getLogger(__name__)


class OllamaService:
    """
    Service for interacting with Ollama API.

    Handles communication with the Ollama API to generate AI responses
    using the configured model.
    """

    def __init__(
        self,
        base_url: Optional[str] = None,
        model: Optional[str] = None,
        timeout: Optional[int] = None
    ):
        """
        Initialize Ollama service.

        Args:
            base_url: Ollama API base URL (defaults to settings.ollama_base_url)
            model: Model name to use (defaults to settings.ollama_model)
            timeout: Request timeout in seconds (defaults to settings.ollama_timeout)
        """
        self.base_url = (base_url or settings.ollama_base_url).rstrip("/")
        self.model = model or settings.ollama_model
        self.timeout = timeout or settings.ollama_timeout
        self.chat_endpoint = f"{self.base_url}/api/chat"

        logger.info(
            f"Initialized OllamaService with base_url={self.base_url}, "
            f"model={self.model}, timeout={self.timeout}s"
        )

    async def generate_response(
        self,
        prompt: str,
        conversation_history: Optional[List[Dict[str, str]]] = None,
        stream: bool = False
    ) -> str:
        """
        Generate AI response for the given prompt.

        Args:
            prompt: User's prompt/question
            conversation_history: Optional list of previous messages for context
                                Format: [{"role": "user", "content": "..."}, {"role": "assistant", "content": "..."}]
            stream: Whether to stream the response (not implemented yet)

        Returns:
            Generated AI response text

        Raises:
            httpx.HTTPError: If the API request fails
            Exception: For other errors during generation
        """
        messages = conversation_history or []

        # Add the current prompt
        messages.append({"role": "user", "content": prompt})

        payload = {
            "model": self.model,
            "messages": messages,
            "stream": stream
        }

        # ═══ CONSOLE LOG: GENERATING AI RESPONSE ═══
        print("\n" + "━" * 80)
        print("🤖 GENERATING AI RESPONSE WITH OLLAMA")
        print("━" * 80)
        print(f"  Model:           {self.model}")
        print(f"  Endpoint:        {self.chat_endpoint}")
        print(f"  Timeout:         {self.timeout}s")
        print(f"  Context Length:  {len(messages) - 1} previous messages")
        print(f"  User Prompt:     {prompt[:100]}{'...' if len(prompt) > 100 else ''}")
        print("━" * 80)
        print("  ⏳ Waiting for Ollama response...")
        print("━" * 80 + "\n")

        logger.debug(f"Sending request to Ollama API: {payload}")

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                response = await client.post(
                    self.chat_endpoint,
                    json=payload
                )
                response.raise_for_status()

                data = response.json()
                logger.debug(f"Ollama API response: {data}")

                # Extract the assistant's message from the response
                if "message" in data and "content" in data["message"]:
                    ai_response = data["message"]["content"]

                    # ═══ CONSOLE LOG: AI RESPONSE GENERATED ═══
                    print("\n" + "━" * 80)
                    print("✅ AI RESPONSE GENERATED SUCCESSFULLY")
                    print("━" * 80)
                    print(f"  Response Length: {len(ai_response)} characters")
                    print(f"  Preview:         {ai_response[:150]}{'...' if len(ai_response) > 150 else ''}")
                    print("━" * 80 + "\n")

                    logger.info(f"Successfully generated response (length: {len(ai_response)})")
                    return ai_response
                else:
                    logger.error(f"Unexpected response format from Ollama: {data}")
                    raise ValueError(f"Unexpected response format: {data}")

        except httpx.TimeoutException as e:
            logger.error(f"Timeout while calling Ollama API: {e}")
            raise Exception(f"Ollama API timeout after {self.timeout}s: {str(e)}")

        except httpx.HTTPStatusError as e:
            logger.error(f"HTTP error from Ollama API: {e.response.status_code} - {e.response.text}")
            raise Exception(f"Ollama API error: {e.response.status_code} - {e.response.text}")

        except httpx.RequestError as e:
            logger.error(f"Request error while calling Ollama API: {e}")
            raise Exception(f"Failed to connect to Ollama API at {self.base_url}: {str(e)}")

        except Exception as e:
            logger.error(f"Unexpected error during AI generation: {e}", exc_info=True)
            raise

    async def health_check(self) -> bool:
        """
        Check if Ollama service is healthy and reachable.

        Returns:
            True if service is healthy, False otherwise
        """
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                response = await client.get(f"{self.base_url}/api/tags")
                response.raise_for_status()
                logger.info("Ollama health check passed")
                return True
        except Exception as e:
            logger.error(f"Ollama health check failed: {e}")
            return False
