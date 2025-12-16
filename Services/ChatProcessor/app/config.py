"""Configuration management for ChatProcessor service."""

import os
from typing import Optional
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """Application settings with environment variable support."""

    # RabbitMQ Configuration
    rabbitmq_host: str = "localhost"
    rabbitmq_port: int = 5672
    rabbitmq_username: str = "guest"
    rabbitmq_password: str = "guest"
    rabbitmq_queue_input: str = "UserPromptReceived"
    rabbitmq_queue_output: str = "BotResponseCreated"

    # Ollama Configuration
    ollama_base_url: str = "http://localhost:11434"
    ollama_model: str = "llama2"
    ollama_timeout: int = 300  # 5 minutes

    # Service Configuration
    log_level: str = "INFO"
    prefetch_count: int = 1  # Process one message at a time

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"
        case_sensitive = False


# Global settings instance
settings = Settings()
