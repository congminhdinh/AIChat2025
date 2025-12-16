from fastapi import FastAPI, HTTPException
from fastapi.concurrency import run_in_threadpool
from pydantic import BaseModel
from optimum.onnxruntime import ORTModelForFeatureExtraction
from transformers import AutoTokenizer
from qdrant_client import QdrantClient
from qdrant_client.models import Distance, VectorParams, PointStruct
import uvicorn
import os
from typing import List, Optional
import uuid
import torch

app = FastAPI(title="VN Law Embedding Service")

MODEL_NAME = os.getenv("MODEL_NAME", "truro7/vn-law-embedding")
QDRANT_HOST = os.getenv("QDRANT_HOST", "localhost")
QDRANT_PORT = int(os.getenv("QDRANT_PORT", "6333"))
QDRANT_COLLECTION = os.getenv("QDRANT_COLLECTION", "vn_law_documents")

print(f"Loading model: {MODEL_NAME}...")
try:
    # Use ONNX Runtime for lighter and faster inference
    tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME)
    model = ORTModelForFeatureExtraction.from_pretrained(MODEL_NAME, export=True)
    print("Model loaded successfully with ONNX Runtime!")
except Exception as e:
    print(f"Error loading model: {e}")

print(f"Connecting to Qdrant at {QDRANT_HOST}:{QDRANT_PORT}...")
try:
    qdrant_client = QdrantClient(host=QDRANT_HOST, port=QDRANT_PORT)
    print("Qdrant client initialized successfully!")
except Exception as e:
    print(f"Error connecting to Qdrant: {e}")

class EmbeddingRequest(BaseModel):
    text: str

class VectorizeRequest(BaseModel):
    text: str
    metadata: dict
    collection_name: Optional[str] = None

class BatchVectorizeRequest(BaseModel):
    items: List[VectorizeRequest]
    collection_name: Optional[str] = None

def mean_pooling(model_output, attention_mask):
    """Mean pooling to get sentence embeddings"""
    token_embeddings = model_output[0]
    input_mask_expanded = attention_mask.unsqueeze(-1).expand(token_embeddings.size()).float()
    return torch.sum(token_embeddings * input_mask_expanded, 1) / torch.clamp(input_mask_expanded.sum(1), min=1e-9)

def encode_text(text: str):
    """Encode text to embedding vector using ONNX model"""
    encoded_input = tokenizer(text, padding=True, truncation=True, return_tensors='pt', max_length=512)
    model_output = model(**encoded_input)
    sentence_embeddings = mean_pooling(model_output, encoded_input['attention_mask'])
    # Normalize embeddings
    sentence_embeddings = torch.nn.functional.normalize(sentence_embeddings, p=2, dim=1)
    return sentence_embeddings[0].tolist()

@app.post("/embed")
async def create_embedding(request: EmbeddingRequest):
    try:
        if not request.text:
            raise HTTPException(status_code=400, detail="Text cannot be empty")

        # Offload CPU-bound inference to thread pool
        embedding = await run_in_threadpool(encode_text, request.text)

        return {
            "vector": embedding,
            "dimensions": len(embedding)
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/vectorize")
async def vectorize_and_store(request: VectorizeRequest):
    """Generate embedding and store directly in Qdrant"""
    try:
        if not request.text:
            raise HTTPException(status_code=400, detail="Text cannot be empty")

        collection_name = request.collection_name or QDRANT_COLLECTION

        # Offload CPU-bound inference to thread pool
        embedding = await run_in_threadpool(encode_text, request.text)

        # Ensure collection exists
        await ensure_collection(collection_name, len(embedding))

        # Generate unique ID
        point_id = str(uuid.uuid4())

        # Store in Qdrant
        qdrant_client.upsert(
            collection_name=collection_name,
            points=[
                PointStruct(
                    id=point_id,
                    vector=embedding,
                    payload={
                        "text": request.text,
                        **request.metadata
                    }
                )
            ]
        )

        return {
            "success": True,
            "point_id": point_id,
            "dimensions": len(embedding),
            "collection": collection_name
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/vectorize-batch")
async def vectorize_batch(request: BatchVectorizeRequest):
    """Generate embeddings for multiple items and store in Qdrant"""
    try:
        if not request.items:
            raise HTTPException(status_code=400, detail="Items list cannot be empty")

        collection_name = request.collection_name or QDRANT_COLLECTION

        points = []
        for item in request.items:
            if not item.text:
                continue

            # Offload CPU-bound inference to thread pool
            embedding = await run_in_threadpool(encode_text, item.text)

            # Ensure collection exists (only once)
            if not points:
                await ensure_collection(collection_name, len(embedding))

            # Generate unique ID
            point_id = str(uuid.uuid4())

            points.append(
                PointStruct(
                    id=point_id,
                    vector=embedding,
                    payload={
                        "text": item.text,
                        **item.metadata
                    }
                )
            )

        # Batch insert to Qdrant
        if points:
            qdrant_client.upsert(
                collection_name=collection_name,
                points=points
            )

        return {
            "success": True,
            "count": len(points),
            "collection": collection_name
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

async def ensure_collection(collection_name: str, vector_size: int):
    """Ensure Qdrant collection exists, create if not"""
    collections = qdrant_client.get_collections().collections
    collection_names = [c.name for c in collections]

    if collection_name not in collection_names:
        qdrant_client.create_collection(
            collection_name=collection_name,
            vectors_config=VectorParams(size=vector_size, distance=Distance.COSINE)
        )

@app.get("/health")
def health_check():
    return {"status": "ok", "model": MODEL_NAME, "qdrant": f"{QDRANT_HOST}:{QDRANT_PORT}"}

if __name__ == "__main__":
    import uvicorn 
    uvicorn.run(app, host="0.0.0.0", port=8000)
