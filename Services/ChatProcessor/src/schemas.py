from datetime import datetime
from typing import Optional, List, Dict
from pydantic import BaseModel, Field, ConfigDict

class PromptConfigDto(BaseModel):
    key: str
    value: str

class UserPromptReceivedMessage(BaseModel):
    model_config = ConfigDict(populate_by_name=True)
    conversation_id: int = Field(alias='conversationId')
    message: str
    user_id: int = Field(alias='userId')
    tenant_id: Optional[int] = Field(default=0, alias='tenantId')
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow)
    system_instruction: Optional[List[PromptConfigDto]] = Field(default_factory=list, alias='systemInstruction')

class BotResponseCreatedMessage(BaseModel):
    model_config = ConfigDict(populate_by_name=True)
    conversation_id: int = Field(alias='conversationId')
    message: str
    user_id: int = Field(alias='userId')
    tenant_id: int = Field(alias='tenantId')
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow)
    model_used: Optional[str] = Field(default=None, alias='modelUsed')

class ChatRequest(BaseModel):
    conversation_id: int
    message: str
    user_id: int
    tenant_id: int
    system_instruction: Optional[List[PromptConfigDto]] = Field(default_factory=list)

class ChatResponse(BaseModel):
    conversation_id: int
    message: str
    user_id: int
    timestamp: datetime
    model_used: str
    rag_documents_used: int
    source_ids: Optional[List] = []
    scenario: Optional[str] = None  # NEW: Scenario for debugging (BOTH, COMPANY_ONLY, LEGAL_ONLY, NONE)