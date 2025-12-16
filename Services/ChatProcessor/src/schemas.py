from datetime import datetime
from typing import Optional, List
from pydantic import BaseModel, Field

class UserPromptReceivedMessage(BaseModel):
    conversation_id: int
    message: str
    user_id: int
    tenant_id: int
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow)

class BotResponseCreatedMessage(BaseModel):
    conversation_id: int
    message: str
    user_id: int
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow)
    model_used: Optional[str] = None

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
    source_ids: Optional[List] = []
