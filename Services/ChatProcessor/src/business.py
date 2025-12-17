import httpx
import asyncio
from typing import List, Dict, Any, Optional
from datetime import datetime
from qdrant_client import QdrantClient
from qdrant_client.models import Filter, FieldCondition, MatchValue

from src.config import settings
from src.logger import logger

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
            # Log detailed error information from Ollama
            error_detail = "Unknown error"
            try:
                error_body = e.response.text
                logger.error(f"HTTP error from Ollama: {e.response.status_code} | Response: {error_body}")
                error_detail = error_body[:200] if error_body else f"Status {e.response.status_code}"
            except:
                logger.error(f"HTTP error from Ollama: {e.response.status_code} (could not read response body)")

            raise Exception(f"Ollama error: {error_detail}")
        except httpx.RequestError as e:
            logger.error(f"Request error calling Ollama: {e}")
            raise Exception(f"Failed to connect to Ollama: {str(e)}")
        except Exception as e:
            logger.error(f"Error during AI generation: {e}", exc_info=True)
            raise

    async def list_models(self) -> list:
        """List all available models in Ollama"""
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                response = await client.get(f"{self.base_url}/api/tags")
                response.raise_for_status()
                data = response.json()
                models = [model.get("name", "") for model in data.get("models", [])]
                return models
        except Exception as e:
            logger.error(f"Failed to list Ollama models: {e}")
            return []

    async def health_check(self) -> bool:
        try:
            models = await self.list_models()
            if models:
                logger.info(f"Ollama health check passed. Available models: {', '.join(models)}")
                if self.model not in models:
                    logger.warning(f"Configured model '{self.model}' not found in Ollama. Available: {', '.join(models)}")
                    logger.warning(f"Please run: ollama pull {self.model}")
                return True
            else:
                logger.warning("Ollama is running but no models found. Please pull a model first.")
                return False
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

    async def search_exact_tenant(self, query_vector: List[float], tenant_id: int, limit: int = 1):
        """Search for documents with exact tenant_id match"""
        search_filter = Filter(
            must=[
                FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id))
            ]
        )
        results = self.client.search(
            collection_name=self.collection_name,
            query_vector=query_vector,
            query_filter=search_filter,
            limit=limit
        )
        logger.info(f"Qdrant exact search completed: tenant_id={tenant_id}, results={len(results)}")
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
    async def process_chat_message(conversation_id: int, user_id: int, message: str, tenant_id: int, ollama_service: OllamaService, qdrant_service: QdrantService, system_instruction: Optional[List[Dict[str, str]]] = None) -> Dict[str, Any]:
        try:
            logger.info(f"[ConversationId: {conversation_id}] Processing message from User {user_id}, Tenant {tenant_id}: '{message[:50]}...'")

            # Get query embedding
            query_embedding = qdrant_service.get_embedding(message)

            # Execute dual queries in parallel:
            # Query A: Legal Base (tenant_id = 1)
            # Query B: Company Rule (tenant_id = current_user_tenant)
            legal_base_task = qdrant_service.search_exact_tenant(
                query_vector=query_embedding,
                tenant_id=1,
                limit=1
            )
            company_rule_task = qdrant_service.search_exact_tenant(
                query_vector=query_embedding,
                tenant_id=tenant_id,
                limit=1
            )

            # Execute both queries in parallel
            legal_base_results, company_rule_results = await asyncio.gather(
                legal_base_task,
                company_rule_task,
                return_exceptions=True
            )

            # Handle potential errors from parallel execution
            if isinstance(legal_base_results, Exception):
                logger.error(f"[ConversationId: {conversation_id}] Legal base query failed: {legal_base_results}")
                legal_base_results = []
            if isinstance(company_rule_results, Exception):
                logger.error(f"[ConversationId: {conversation_id}] Company rule query failed: {company_rule_results}")
                company_rule_results = []

            # Build context with labels
            context_sections = []
            source_ids = []
            documents_used = 0

            # Process legal base results
            if legal_base_results and len(legal_base_results) > 0:
                for result in legal_base_results:
                    if hasattr(result, 'payload') and 'text' in result.payload:
                        context_sections.append(f"[STATE LAW]\n{result.payload['text']}")
                        if 'source_id' in result.payload:
                            source_ids.append(result.payload['source_id'])
                        documents_used += 1
                        logger.info(f"[ConversationId: {conversation_id}] Retrieved STATE LAW document")
            else:
                logger.warning(f"[ConversationId: {conversation_id}] No STATE LAW documents found")

            # Process company rule results
            if company_rule_results and len(company_rule_results) > 0:
                for result in company_rule_results:
                    if hasattr(result, 'payload') and 'text' in result.payload:
                        context_sections.append(f"[COMPANY REGULATION]\n{result.payload['text']}")
                        if 'source_id' in result.payload:
                            source_ids.append(result.payload['source_id'])
                        documents_used += 1
                        logger.info(f"[ConversationId: {conversation_id}] Retrieved COMPANY REGULATION document")
            else:
                logger.warning(f"[ConversationId: {conversation_id}] No COMPANY REGULATION documents found for tenant {tenant_id}")

            # Build enhanced prompt with context
            if context_sections:
                context = "\n\n".join(context_sections)
                enhanced_prompt = f"""Context information:
{context}

User question: {message}

Please answer based on the context provided above. If both STATE LAW and COMPANY REGULATION are provided, compare and contrast them in your response."""
            else:
                logger.warning(f"[ConversationId: {conversation_id}] No documents retrieved from either source. Using raw query.")
                enhanced_prompt = message

            # Build conversation history with system instruction if provided
            conversation_history = []
            if system_instruction and len(system_instruction) > 0:
                # Concatenate all Values to construct dynamic system prompt
                system_prompt_parts = [item['value'] for item in system_instruction if 'value' in item]
                if system_prompt_parts:
                    dynamic_system_prompt = "\n\n".join(system_prompt_parts)
                    conversation_history.append({"role": "system", "content": dynamic_system_prompt})
                    logger.info(f"[ConversationId: {conversation_id}] Applied dynamic system instruction with {len(system_prompt_parts)} parts")

            ai_response = await ollama_service.generate_response(prompt=enhanced_prompt, conversation_history=conversation_history if conversation_history else None)
            logger.info(f"[ConversationId: {conversation_id}] Generated response (length: {len(ai_response)})")

            return {
                "conversation_id": conversation_id,
                "message": ai_response,
                "user_id": 0,
                "timestamp": datetime.utcnow(),
                "model_used": ollama_service.model,
                "rag_documents_used": documents_used,
                "source_ids": source_ids
            }

        except Exception as e:
            logger.error(f"[ConversationId: {conversation_id}] Failed to process message: {e}", exc_info=True)
            raise
