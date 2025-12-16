import logging
from typing import Optional, Dict, Any
from datetime import datetime
from app.services.ollama_service import OllamaService
from app.services.qdrant_service import QdrantService
from app.config import settings

logger = logging.getLogger(__name__)


async def process_chat_message(
    conversation_id: int,
    user_id: int,
    message: str,
    tenant_id: int,
    ollama_service: OllamaService,
    qdrant_service: QdrantService
) -> Dict[str, Any]:
    try:
        logger.info(
            f"[ConversationId: {conversation_id}] Processing message from User {user_id}, "
            f"Tenant {tenant_id}: '{message[:50]}...'"
        )

        query_embedding = qdrant_service.get_embedding(message)

        rag_results = await qdrant_service.search_with_tenant_filter(
            query_vector=query_embedding,
            tenant_id=tenant_id,
            limit=settings.rag_top_k
        )

        context_texts = []
        for result in rag_results:
            if hasattr(result, 'payload') and 'text' in result.payload:
                context_texts.append(result.payload['text'])

        if context_texts:
            context = "\n\n".join(context_texts)
            enhanced_prompt = f"""Context information:
{context}

User question: {message}

Please answer based on the context provided above."""
        else:
            enhanced_prompt = message

        ai_response = await ollama_service.generate_response(
            prompt=enhanced_prompt,
            conversation_history=None
        )

        logger.info(
            f"[ConversationId: {conversation_id}] Generated response "
            f"(length: {len(ai_response)})"
        )

        return {
            "conversation_id": conversation_id,
            "message": ai_response,
            "user_id": 0,
            "timestamp": datetime.utcnow(),
            "model_used": ollama_service.model,
            "rag_documents_used": len(context_texts)
        }

    except Exception as e:
        logger.error(
            f"[ConversationId: {conversation_id}] Failed to process message: {e}",
            exc_info=True
        )
        raise
