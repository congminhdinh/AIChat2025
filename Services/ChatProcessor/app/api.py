import logging
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Optional
from datetime import datetime
from app.services.service import process_chat_message
from app.services.ollama_service import OllamaService
from app.services.qdrant_service import QdrantService
logger = logging.getLogger(__name__)
app = FastAPI(title='ChatProcessor API', version='1.0.0')
ollama_service = OllamaService()
qdrant_service = QdrantService()

class ChatRequest(BaseModel):
    conversation_id: int
    message: str
    user_id: int
    tenant_id: int

class ChatResponse(BaseModel):
    conversation_id: int
    message: str
    user_id: int
    timestamp: datetime
    model_used: str
    rag_documents_used: int

@app.get('/health')
async def health_check():
    ollama_healthy = await ollama_service.health_check()
    qdrant_healthy = qdrant_service.health_check()
    return {'status': 'healthy' if ollama_healthy and qdrant_healthy else 'degraded', 'ollama': ollama_healthy, 'qdrant': qdrant_healthy}

@app.post('/api/chat/test', response_model=ChatResponse)
async def test_chat(request: ChatRequest):
    try:
        result = await process_chat_message(conversation_id=request.conversation_id, user_id=request.user_id, message=request.message, tenant_id=request.tenant_id, ollama_service=ollama_service, qdrant_service=qdrant_service)
        return ChatResponse(**result)
    except Exception as e:
        logger.error(f'Error processing chat request: {e}', exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))