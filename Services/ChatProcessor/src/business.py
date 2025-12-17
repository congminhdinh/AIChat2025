import logging
import httpx
from typing import List, Dict, Any, Optional
from datetime import datetime
from qdrant_client import QdrantClient
from qdrant_client.models import Filter, FieldCondition, MatchValue

from src.config import settings

logger = logging.getLogger(__name__)

class OllamaService:
    def __init__(self, base_url: Optional[str] = None, model: Optional[str] = None, timeout: Optional[int] = None):
        self.base_url = (base_url or settings.ollama_base_url).rstrip("/")
        self.model = model or settings.ollama_model
        self.timeout = timeout or settings.ollama_timeout
        self.chat_endpoint = f"{self.base_url}/api/chat"
        logger.info(f"Initialized OllamaService: base_url={self.base_url}, model={self.model}, timeout={self.timeout}s")

    async def generate_response(self, prompt: str, conversation_history: Optional[List[Dict[str, str]]] = None, stream: bool = False) -> str:
        messages = conversation_history or []
        messages.append({"role": "user", "content": prompt})
        payload = {"model": self.model, "messages": messages, "stream": stream}
        logger.debug(f"Sending request to Ollama: {payload}")

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                response = await client.post(self.chat_endpoint, json=payload)
                response.raise_for_status()
                data = response.json()

                if "message" in data and "content" in data["message"]:
                    ai_response = data["message"]["content"]
                    logger.info(f"Generated response (length: {len(ai_response)})")
                    return ai_response
                else:
                    logger.error(f"Unexpected response format: {data}")
                    raise ValueError(f"Unexpected response format: {data}")

        except httpx.TimeoutException as e:
            logger.error(f"Timeout calling Ollama: {e}")
            raise Exception(f"Ollama timeout after {self.timeout}s: {str(e)}")
        except httpx.HTTPStatusError as e:
            logger.error(f"HTTP error from Ollama: {e.response.status_code}")
            raise Exception(f"Ollama error: {e.response.status_code}")
        except httpx.RequestError as e:
            logger.error(f"Request error calling Ollama: {e}")
            raise Exception(f"Failed to connect to Ollama: {str(e)}")
        except Exception as e:
            logger.error(f"Error during AI generation: {e}", exc_info=True)
            raise

    async def health_check(self) -> bool:
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                response = await client.get(f"{self.base_url}/api/tags")
                response.raise_for_status()
                logger.info("Ollama health check passed")
                return True
        except Exception as e:
            logger.error(f"Ollama health check failed: {e}")
            return False

class QdrantService:
    def __init__(self, host: Optional[str] = None, port: Optional[int] = None, collection_name: Optional[str] = None):
        self.host = host or settings.qdrant_host
        self.port = port or settings.qdrant_port
        self.collection_name = collection_name or settings.qdrant_collection
        self.client = QdrantClient(host=self.host, port=self.port)
        logger.info(f"Initialized QdrantService: {self.host}:{self.port}, collection={self.collection_name}")

    async def search_with_tenant_filter(self, query_vector: List[float], tenant_id: int, limit: int = 5):
        search_filter = Filter(
            should=[
                FieldCondition(key="tenant_id", match=MatchValue(value=1)),
                FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id))
            ]
        )
        results = self.client.search(collection_name=self.collection_name, query_vector=query_vector, query_filter=search_filter, limit=limit)
        logger.info(f"Qdrant search completed: tenant_id={tenant_id}, results={len(results)}")
        return results

    def get_embedding(self, text: str) -> List[float]:
        try:
            response = httpx.post(
                f"{settings.embedding_service_url}/embed",
                json={"text": text},
                timeout=30.0
            )
            response.raise_for_status()
            result = response.json()
            return result["vector"]
        except Exception as e:
            logger.error(f"Failed to get embedding from service: {e}")
            raise

    def health_check(self) -> bool:
        try:
            collections = self.client.get_collections()
            logger.info("Qdrant health check passed")
            return True
        except Exception as e:
            logger.error(f"Qdrant health check failed: {e}")
            return False

class ChatBusiness:
    @staticmethod
    async def process_chat_message(conversation_id: int, user_id: int, message: str, tenant_id: int, ollama_service: OllamaService, qdrant_service: QdrantService) -> Dict[str, Any]:
        try:
            logger.info(f"[ConversationId: {conversation_id}] Processing message from User {user_id}, Tenant {tenant_id}: '{message[:50]}...'")

            query_embedding = qdrant_service.get_embedding(message)
            rag_results = await qdrant_service.search_with_tenant_filter(query_vector=query_embedding, tenant_id=tenant_id, limit=settings.rag_top_k)

            context_texts = []
            source_ids = []
            for result in rag_results:
                if hasattr(result, 'payload') and 'text' in result.payload:
                    context_texts.append(result.payload['text'])
                    if 'source_id' in result.payload:
                        source_ids.append(result.payload['source_id'])

            if context_texts:
                context = "\n\n".join(context_texts)
                enhanced_prompt = f"""Context information:
{context}

User question: {message}

Please answer based on the context provided above."""
            else:
                enhanced_prompt = message

            ai_response = await ollama_service.generate_response(prompt=enhanced_prompt, conversation_history=None)
            logger.info(f"[ConversationId: {conversation_id}] Generated response (length: {len(ai_response)})")

            return {
                "conversation_id": conversation_id,
                "message": ai_response,
                "user_id": 0,
                "timestamp": datetime.utcnow(),
                "model_used": ollama_service.model,
                "rag_documents_used": len(context_texts),
                "source_ids": source_ids
            }

        except Exception as e:
            logger.error(f"[ConversationId: {conversation_id}] Failed to process message: {e}", exc_info=True)
            raise
