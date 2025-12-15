from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer
from qdrant_client import QdrantClient
from qdrant_client.models import Distance, VectorParams, PointStruct
import uvicorn
import os
from typing import List, Optional
import uuid

app = FastAPI(title="VN Law Embedding Service")

MODEL_NAME = os.getenv("MODEL_NAME", "truro7/vn-law-embedding")
QDRANT_HOST = os.getenv("QDRANT_HOST", "localhost")
QDRANT_PORT = int(os.getenv("QDRANT_PORT", "6333"))
QDRANT_COLLECTION = os.getenv("QDRANT_COLLECTION", "vn_law_documents")

print(f"Loading model: {MODEL_NAME}...")
try:
    model = SentenceTransformer(MODEL_NAME)
    print("Model loaded successfully!")
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

@app.post("/embed")
async def create_embedding(request: EmbeddingRequest):
    try:
        if not request.text:
            raise HTTPException(status_code=400, detail="Text cannot be empty")

        embedding = model.encode(request.text).tolist()

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

        # Generate embedding
        embedding = model.encode(request.text).tolist()

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

            # Generate embedding
            embedding = model.encode(item.text).tolist()

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
