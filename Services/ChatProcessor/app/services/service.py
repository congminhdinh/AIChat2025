import logging
import re
from typing import Optional, Dict, Any
from datetime import datetime
from app.services.ollama_service import OllamaService
from app.services.qdrant_service import QdrantService
from app.config import settings
logger = logging.getLogger(__name__)

def _cleanup_response(response: str) -> str:
    """Remove Vietnamese prefixes and reasoning steps from LLM response."""
    cleaned = response.strip()

    # Remove chain-of-thought reasoning (Bước 1, Bước 2, etc.)
    steps = re.split(r'Bước \d+:', cleaned)
    if len(steps) > 1:
        cleaned = steps[-1].strip()
        logger.debug(f'Removed {len(steps)-1} reasoning steps')

    # Remove common prefixes
    prefixes = ["Trả lời:", "Câu trả lời:", "Đáp án:", "Answer:"]
    for prefix in prefixes:
        if cleaned.lower().startswith(prefix.lower()):
            cleaned = cleaned[len(prefix):].strip()
            break

    return cleaned

async def process_chat_message(conversation_id: int, user_id: int, message: str, tenant_id: int, ollama_service: OllamaService, qdrant_service: QdrantService) -> Dict[str, Any]:
    try:
        logger.info(f"[ConversationId: {conversation_id}] Processing message from User {user_id}, Tenant {tenant_id}: '{message[:50]}...'")
        query_embedding = qdrant_service.get_embedding(message)
        rag_results = await qdrant_service.search_with_tenant_filter(query_vector=query_embedding, tenant_id=tenant_id, limit=settings.rag_top_k)

        # Separate results by tenant_id (source)
        company_results = []
        legal_results = []

        for result in rag_results:
            if hasattr(result, 'payload'):
                result_tenant_id = result.payload.get('tenant_id', tenant_id)
                if result_tenant_id == 1:
                    legal_results.append(result)
                else:
                    company_results.append(result)

        # Detect scenario based on actual results
        scenario = qdrant_service.detect_scenario(company_results, legal_results)
        logger.info(f'[ConversationId: {conversation_id}] Detected scenario: {scenario}')

        # Handle NONE scenario early
        if scenario == "NONE":
            logger.warning(f'[ConversationId: {conversation_id}] No documents found')
            return {
                'conversation_id': conversation_id,
                'message': 'Xin lỗi, hệ thống không tìm thấy thông tin liên quan đến câu hỏi của bạn.',
                'user_id': 0,
                'tenant_id': tenant_id,
                'timestamp': datetime.utcnow(),
                'model_used': ollama_service.model,
                'rag_documents_used': 0,
                'source_ids': [],
                'scenario': scenario
            }

        context_texts = []
        source_ids = []
        for result in rag_results:
            if hasattr(result, 'payload'):
                payload = result.payload
                result_tenant_id = payload.get('tenant_id', tenant_id)

                # Format context with proper citations
                if 'document_name' in payload:
                    # Structured document format (both Legal Law and Company Law)
                    document_name = payload.get('document_name', '')
                    heading1 = payload.get('heading1', '')
                    heading2 = payload.get('heading2', '')
                    content = payload.get('content', '')

                    # Build formatted citation
                    citation_parts = []

                    # Add document source indicator
                    if result_tenant_id == 1:
                        # Legal Law (Vietnamese legislation)
                        if document_name:
                            citation_parts.append(f'[Luật: {document_name}]')
                    else:
                        # Company Law (Company-specific policy)
                        if document_name:
                            citation_parts.append(f'[Nội quy Công ty: {document_name}]')

                    # Add heading information
                    if heading1 and heading2:
                        citation_parts.append(f'{heading1} - {heading2}')
                    elif heading1:
                        citation_parts.append(heading1)

                    # Combine citation and content
                    if citation_parts and content:
                        formatted_text = ' '.join(citation_parts) + f': {content}'
                    elif content:
                        formatted_text = content
                    else:
                        formatted_text = payload.get('text', '')

                    context_texts.append(formatted_text)
                elif 'text' in payload:
                    # Fallback: use plain text
                    context_texts.append(payload['text'])

                # Track source IDs
                if 'source_id' in payload:
                    source_ids.append(payload['source_id'])

        # Select system prompt based on SCENARIO (not just tenant_id)
        if scenario == "BOTH":
            system_prompt = settings.system_prompt_comparison
            logger.info(f'[ConversationId: {conversation_id}] Using COMPARISON prompt for BOTH scenario')
        elif scenario in ["COMPANY_ONLY", "LEGAL_ONLY"]:
            # For single-source scenarios
            if scenario == "LEGAL_ONLY" and tenant_id == 1:
                # Tenant 1 with only legal docs uses specialized legal prompt
                system_prompt = settings.system_prompt_legal
            else:
                system_prompt = settings.system_prompt_single_source
            logger.info(f'[ConversationId: {conversation_id}] Using SINGLE SOURCE prompt for {scenario}')
        else:
            # Fallback (shouldn't reach here due to NONE handling above)
            system_prompt = settings.system_prompt_default
            logger.warning(f'[ConversationId: {conversation_id}] Using default prompt for unexpected scenario: {scenario}')

        # Build conversation history with system prompt
        conversation_history = [
            {'role': 'system', 'content': system_prompt}
        ]

        # Build the user prompt
        if context_texts:
            context = '\n\n'.join(context_texts)
            enhanced_prompt = f'Context information:\n{context}\n\nUser question: {message}\n\nPlease answer based on the context provided above.'
        else:
            enhanced_prompt = message

        ai_response = await ollama_service.generate_response(prompt=enhanced_prompt, conversation_history=conversation_history, temperature=0.1)
        logger.info(f'[ConversationId: {conversation_id}] Generated response (length: {len(ai_response)})')

        # Post-processing cleanup
        ai_response = _cleanup_response(ai_response)
        logger.info(f'[ConversationId: {conversation_id}] Response cleaned (length: {len(ai_response)})')

        return {'conversation_id': conversation_id, 'message': ai_response, 'user_id': 0, 'tenant_id': tenant_id, 'timestamp': datetime.utcnow(), 'model_used': ollama_service.model, 'rag_documents_used': len(context_texts), 'source_ids': source_ids, 'scenario': scenario}
    except Exception as e:
        logger.error(f'[ConversationId: {conversation_id}] Failed to process message: {e}', exc_info=True)
        raise