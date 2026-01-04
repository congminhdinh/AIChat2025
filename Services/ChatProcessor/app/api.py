import logging
import asyncio
import json
from pathlib import Path
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Optional, List
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

class TestEntity(BaseModel):
    tenant_id: int
    TC_id: str
    questions: str

class BatchTestRequest(BaseModel):
    entities: List[TestEntity]

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

async def process_batch_in_background(entities: List[TestEntity]):
    """Background task to process batch test entities."""
    results = []
    logger.info(f'Starting background processing of {len(entities)} entities')

    for idx, entity in enumerate(entities):
        try:
            logger.info(f'Processing entity {idx + 1}/{len(entities)}: TC_id={entity.TC_id}, tenant_id={entity.tenant_id}')

            # Use conversation_id=0 and user_id=0 for test requests
            result = await process_chat_message(
                conversation_id=0,
                user_id=0,
                message=entity.questions,
                tenant_id=entity.tenant_id,
                ollama_service=ollama_service,
                qdrant_service=qdrant_service
            )

            # Collect result with TC_id and answer
            results.append({
                'TC_id': entity.TC_id,
                'answer': result.get('message', '')
            })

            logger.info(f'Completed entity {idx + 1}/{len(entities)}: TC_id={entity.TC_id}')

            # Wait 3 seconds before processing next entity
            if idx < len(entities) - 1:  # Don't wait after the last entity
                logger.info(f'Waiting 1 seconds before next entity...')
                await asyncio.sleep(1)

        except Exception as e:
            logger.error(f'Error processing entity TC_id={entity.TC_id}: {e}', exc_info=True)
            results.append({
                'TC_id': entity.TC_id,
                'answer': f'Error: {str(e)}'
            })

    # Write results to tdd.json file
    try:
        output_file = Path('tdd.json')
        with output_file.open('w', encoding='utf-8') as f:
            json.dump(results, f, ensure_ascii=False, indent=2)
        logger.info(f'Successfully wrote {len(results)} results to {output_file}')
    except Exception as e:
        logger.error(f'Failed to write results to file: {e}', exc_info=True)

@app.post('/api/test')
async def batch_test(request: BatchTestRequest):
    """
    Batch test endpoint that processes entities in the background.
    Returns immediately with 202 Accepted status.
    Results are written to tdd.json after processing completes.
    """
    logger.info(f'Received batch test request with {len(request.entities)} entities')

    # Spawn background task
    asyncio.create_task(process_batch_in_background(request.entities))

    # Return immediately
    return {
        'status': 'accepted',
        'message': f'Processing {len(request.entities)} entities in background',
        'output_file': 'tdd.json'
    }