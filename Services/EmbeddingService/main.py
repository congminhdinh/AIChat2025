from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer
import uvicorn
import os

app = FastAPI(title="VN Law Embedding Service")

MODEL_NAME = os.getenv("MODEL_NAME", "truro7/vn-law-embedding")

print(f"Loading model: {MODEL_NAME}...")
try:
    model = SentenceTransformer(MODEL_NAME)
    print("Model loaded successfully!")
except Exception as e:
    print(f"Error loading model: {e}")

class EmbeddingRequest(BaseModel):
    text: str

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

@app.get("/health")
def health_check():
    return {"status": "ok", "model": MODEL_NAME}

if __name__ == "__main__":
    import uvicorn 
    uvicorn.run(app, host="0.0.0.0", port=8000)
