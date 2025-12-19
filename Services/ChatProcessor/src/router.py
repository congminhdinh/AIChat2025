from fastapi import APIRouter, HTTPException
from src.schemas import ChatRequest, ChatResponse
from src.business import ChatBusiness, OllamaService, QdrantService
from src.evaluation_service import get_evaluation_service
from src.logger import logger
router = APIRouter()
ollama_service = OllamaService()
qdrant_service = QdrantService()

@router.get('/health')
async def health_check():
    ollama_healthy = await ollama_service.health_check()
    qdrant_healthy = await qdrant_service.health_check()
    return {'status': 'healthy' if ollama_healthy and qdrant_healthy else 'degraded', 'ollama': ollama_healthy, 'qdrant': qdrant_healthy}

@router.post('/api/chat/test', response_model=ChatResponse)
async def test_chat(request: ChatRequest):
    try:
        result = await ChatBusiness.process_chat_message(
            conversation_id=request.conversation_id,
            user_id=request.user_id,
            message=request.message,
            tenant_id=request.tenant_id,
            ollama_service=ollama_service,
            qdrant_service=qdrant_service,
            system_instruction=request.system_instruction
        )
        return ChatResponse(**result)
    except Exception as e:
        logger.error(f'Error processing chat request: {e}', exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@router.post('/evaluate-batch')
async def evaluate_batch():

    try:
        evaluation_service = get_evaluation_service(
            input_file="evaluation_logs.json",
            output_file="chat_logs_scored.json"
        )

        # Run the evaluation process
        summary = await evaluation_service.run_evaluation()

        return summary

    except FileNotFoundError as e:
        logger.error(f'Input file not found: {e}')
        raise HTTPException(status_code=404, detail=str(e))
    except Exception as e:
        logger.error(f'Error during batch evaluation: {e}', exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))