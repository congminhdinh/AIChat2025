from pydantic import BaseModel
from typing import List, Optional

class EmbeddingRequest(BaseModel):
    text: str

class VectorizeRequest(BaseModel):
    text: str
    metadata: dict
    collection_name: Optional[str] = None

class BatchVectorizeRequest(BaseModel):
    items: List[VectorizeRequest]
    collection_name: Optional[str] = None

class DeleteRequest(BaseModel):
    source_id: str
    tenant_id: int
    type: int
    collection_name: Optional[str] = None

class EmbeddingResponse(BaseModel):
    vector: List[float]
    dimensions: int

class VectorizeResponse(BaseModel):
    success: bool
    point_id: Optional[str] = None
    count: Optional[int] = None
    dimensions: Optional[int] = None
    collection: str
    message: Optional[str] = None
