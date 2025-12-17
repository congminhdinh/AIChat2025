from datetime import datetime
from typing import Optional, List
from pydantic import BaseModel, Field, ConfigDict

class UserPromptReceivedMessage(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    conversation_id: int = Field(alias="conversationId")
    message: str
    user_id: int = Field(alias="userId")
    tenant_id: Optional[int] = Field(default=0, alias="tenantId")  # Optional for backwards compatibility
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow)

class BotResponseCreatedMessage(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    conversation_id: int = Field(alias="conversationId")
    message: str
    user_id: int = Field(alias="userId")
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow)
    model_used: Optional[str] = Field(default=None, alias="modelUsed")

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
