from fastapi import APIRouter, HTTPException
from src.schemas import ChatRequest, ChatResponse
from src.business import ChatBusiness, OllamaService, QdrantService
from src.logger import logger
router = APIRouter()
ollama_service = OllamaService()
qdrant_service = QdrantService()

@router.get('/health')
async def health_check():
    ollama_healthy = await ollama_service.health_check()
    qdrant_healthy = qdrant_service.health_check()
    return {'status': 'healthy' if ollama_healthy and qdrant_healthy else 'degraded', 'ollama': ollama_healthy, 'qdrant': qdrant_healthy}

@router.post('/api/chat/test', response_model=ChatResponse)
async def test_chat(request: ChatRequest):
    try:
        result = await ChatBusiness.process_chat_message(conversation_id=request.conversation_id, user_id=request.user_id, message=request.message, tenant_id=request.tenant_id, ollama_service=ollama_service, qdrant_service=qdrant_service)
        return ChatResponse(**result)
    except Exception as e:
        logger.error(f'Error processing chat request: {e}', exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))