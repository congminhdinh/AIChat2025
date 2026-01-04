"""
DEPRECATED: This module is deprecated. Please use src.business.ChatBusiness instead.

This module is kept for backward compatibility with app/main.py and app/api.py,
but it now delegates to the new hybrid search implementation in src/business.py.

Migration path:
- Old: from app.services.service import process_chat_message
- New: from src.business import ChatBusiness; ChatBusiness.process_chat_message(...)
"""

import logging
from typing import Optional, Dict, Any, List
from datetime import datetime
from app.services.ollama_service import OllamaService
from app.services.qdrant_service import QdrantService

# Import the new hybrid search implementation
from src.business import ChatBusiness, OllamaService as NewOllamaService, QdrantService as NewQdrantService

logger = logging.getLogger(__name__)

# Adapter instances to bridge old and new services
_new_ollama_service = None
_new_qdrant_service = None


def _get_new_ollama_service(old_service: OllamaService) -> NewOllamaService:
    """Adapter to convert old OllamaService to new OllamaService."""
    global _new_ollama_service
    if _new_ollama_service is None:
        _new_ollama_service = NewOllamaService(
            base_url=old_service.base_url,
            model=old_service.model,
            timeout=old_service.timeout
        )
    return _new_ollama_service


def _get_new_qdrant_service(old_service: QdrantService) -> NewQdrantService:
    """Adapter to convert old QdrantService to new QdrantService."""
    global _new_qdrant_service
    if _new_qdrant_service is None:
        _new_qdrant_service = NewQdrantService(
            host=old_service.host,
            port=old_service.port,
            collection_name=old_service.collection_name
        )
    return _new_qdrant_service


async def process_chat_message(
    conversation_id: int,
    user_id: int,
    message: str,
    tenant_id: int,
    ollama_service: OllamaService,
    qdrant_service: QdrantService,
    system_instruction: Optional[List[Dict[str, str]]] = None,
    system_prompt: Optional[str] = None
) -> Dict[str, Any]:
    """
    DEPRECATED: Wrapper that delegates to the new ChatBusiness.process_chat_message().

    This function maintains backward compatibility while using the new hybrid search implementation.
    All calls are forwarded to src.business.ChatBusiness which implements:
    - Hybrid Search (Vector + BM25 Keyword)
    - Legal Term Extraction with abbreviation expansion
    - RRF Re-ranking
    - Adaptive similarity thresholds
    - Fallback mechanism

    Args:
        conversation_id: Conversation identifier
        user_id: User identifier
        message: User query message
        tenant_id: Tenant identifier
        ollama_service: Old Ollama service instance (will be adapted)
        qdrant_service: Old Qdrant service instance (will be adapted)
        system_instruction: Optional list of key-value terminology mappings
        system_prompt: Optional tenant-specific system prompt

    Returns:
        Dictionary containing response message and metadata
    """
    try:
        logger.warning(
            "DEPRECATION WARNING: app.services.service.process_chat_message() is deprecated. "
            "Please migrate to src.business.ChatBusiness.process_chat_message()"
        )

        # Adapt old services to new services
        new_ollama = _get_new_ollama_service(ollama_service)
        new_qdrant = _get_new_qdrant_service(qdrant_service)

        # Delegate to the new hybrid search implementation
        result = await ChatBusiness.process_chat_message(
            conversation_id=conversation_id,
            user_id=user_id,
            message=message,
            tenant_id=tenant_id,
            ollama_service=new_ollama,
            qdrant_service=new_qdrant,
            system_instruction=system_instruction,
            system_prompt=system_prompt
        )

        logger.info(
            f'[ConversationId: {conversation_id}] Processed via NEW hybrid search implementation '
            f'(fallback: {result.get("fallback_triggered", False)})'
        )

        return result

    except Exception as e:
        logger.error(
            f'[ConversationId: {conversation_id}] Failed to process message via hybrid search: {e}',
            exc_info=True
        )
        raise