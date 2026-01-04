from datetime import datetime
from typing import Optional, List, Dict
from pydantic import BaseModel, Field, ConfigDict

class PromptConfigDto(BaseModel):
    key: str
    value: str

class UserPromptReceivedMessage(BaseModel):
    model_config = ConfigDict(populate_by_name=True)
    conversation_id: int = Field(alias='conversationId')
    message_id: int = Field(alias='messageId')  # NEW: Maps to MessageId from C# event
    message: str
    token: str
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow)
    system_instruction: Optional[List[PromptConfigDto]] = Field(default_factory=list, alias='systemInstruction')
    system_prompt: Optional[str] = Field(default=None, alias='systemPrompt')

class BotResponseCreatedMessage(BaseModel):
    model_config = ConfigDict(populate_by_name=True, protected_namespaces=())
    conversation_id: int = Field(alias='conversationId')
    request_id: int = Field(alias='requestId')  # NEW: Maps to RequestId (original MessageId)
    reference_doc_id_list: List[int] = Field(default_factory=list, alias='referenceDocIdList')  # NEW: List of source_id from Qdrant
    message: str
    token: str
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow)
    model_used: Optional[str] = Field(default=None, alias='modelUsed')

class ChatRequest(BaseModel):
    conversation_id: int
    message: str
    user_id: int
    tenant_id: int
    system_instruction: Optional[List[PromptConfigDto]] = Field(default_factory=list)

class ChatResponse(BaseModel):
    model_config = ConfigDict(protected_namespaces=())
    conversation_id: int
    message: str
    user_id: int
    timestamp: datetime
    model_used: str
    rag_documents_used: int
    source_ids: Optional[List] = []
    scenario: Optional[str] = None  # NEW: Scenario for debugging (BOTH, COMPANY_ONLY, LEGAL_ONLY, NONE)

class TestEntity(BaseModel):
    tenant_id: int
    TC_id: str
    questions: str

class BatchTestRequest(BaseModel):
    entities: List[TestEntity]