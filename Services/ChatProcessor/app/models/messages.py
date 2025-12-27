from datetime import datetime
from typing import Optional
from pydantic import BaseModel, Field

class UserPromptReceivedMessage(BaseModel):
    conversation_id: int
    message: str
    user_id: int
    tenant_id: int
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow)

class BotResponseCreatedMessage(BaseModel):
    model_config = {'protected_namespaces': ()}
    conversation_id: int
    message: str
    user_id: int
    tenant_id: int
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow)
    model_used: Optional[str] = None