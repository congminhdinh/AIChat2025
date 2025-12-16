"""Pydantic models for RabbitMQ message contracts."""

from datetime import datetime
from typing import Optional
from pydantic import BaseModel, Field


class UserPromptReceivedMessage(BaseModel):
    """
    Message received from RabbitMQ UserPromptReceived queue.

    This represents a user prompt that needs to be processed by the AI.
    """

    conversation_id: int = Field(..., description="ID of the conversation")
    message: str = Field(..., description="User's prompt/question")
    user_id: int = Field(..., description="ID of the user who sent the message")
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow, description="Message timestamp")

    class Config:
        json_schema_extra = {
            "example": {
                "conversation_id": 123,
                "message": "What is the capital of France?",
                "user_id": 456,
                "timestamp": "2025-12-16T10:30:00Z"
            }
        }


class BotResponseCreatedMessage(BaseModel):
    """
    Message to be published to RabbitMQ BotResponseCreated queue.

    This represents the AI's response to a user prompt.
    CRITICAL: Must include conversation_id so the .NET service knows which chat to update.
    """

    conversation_id: int = Field(..., description="ID of the conversation (REQUIRED for routing)")
    message: str = Field(..., description="AI-generated response")
    user_id: int = Field(..., description="ID of the user (bot/system user)")
    timestamp: Optional[datetime] = Field(default_factory=datetime.utcnow, description="Response timestamp")
    model_used: Optional[str] = Field(None, description="Name of the AI model used")

    class Config:
        json_schema_extra = {
            "example": {
                "conversation_id": 123,
                "message": "The capital of France is Paris.",
                "user_id": 0,
                "timestamp": "2025-12-16T10:30:05Z",
                "model_used": "llama2"
            }
        }
