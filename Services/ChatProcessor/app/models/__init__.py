"""Data models for RabbitMQ messages."""

from .messages import UserPromptReceivedMessage, BotResponseCreatedMessage

__all__ = ["UserPromptReceivedMessage", "BotResponseCreatedMessage"]
