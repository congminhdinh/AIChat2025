from fastapi import APIRouter, HTTPException
from fastapi.concurrency import run_in_threadpool
from src.schemas import EmbeddingRequest, EmbeddingResponse, VectorizeRequest, VectorizeResponse, BatchVectorizeRequest, DeleteRequest
from src.business import EmbeddingService
from src.config import settings
router = APIRouter()
embedding_service = EmbeddingService()

@router.post('/embed', response_model=EmbeddingResponse)
async def create_embedding(request: EmbeddingRequest):
    try:
        embedding = await run_in_threadpool(embedding_service.create_embedding, request.text)
        return EmbeddingResponse(vector=embedding, dimensions=len(embedding))
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post('/vectorize', response_model=VectorizeResponse)
async def vectorize_and_store(request: VectorizeRequest):
    try:
        (point_id, dimensions, collection) = await run_in_threadpool(embedding_service.vectorize_and_store, request.text, request.metadata, request.collection_name)
        return VectorizeResponse(success=True, point_id=point_id, dimensions=dimensions, collection=collection)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post('/vectorize-batch', response_model=VectorizeResponse)
async def vectorize_batch(request: BatchVectorizeRequest):
    try:
        (count, collection) = await run_in_threadpool(embedding_service.vectorize_batch, request.items, request.collection_name)
        return VectorizeResponse(success=True, count=count, collection=collection)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post('/api/embeddings/delete', response_model=VectorizeResponse)
async def delete_document(request: DeleteRequest):
    try:
        collection = await run_in_threadpool(embedding_service.delete_by_filter, request.source_id, request.tenant_id, request.type, request.collection_name)
        return VectorizeResponse(success=True, collection=collection, message=f'Deleted vectors for source_id={request.source_id}, tenant_id={request.tenant_id}, type={request.type}')
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.get('/health')
def health_check():
    return {'status': 'ok', 'model': settings.model_name, 'qdrant': f'{settings.qdrant_host}:{settings.qdrant_port}'}