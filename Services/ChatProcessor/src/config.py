from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    rabbitmq_host: str = "localhost"
    rabbitmq_port: int = 5672
    rabbitmq_username: str = "guest"
    rabbitmq_password: str = "guest"
    rabbitmq_queue_input: str = "UserPromptReceived"
    rabbitmq_queue_output: str = "BotResponseCreated"

    ollama_base_url: str = "http://localhost:11434"
    ollama_model: str = "llama2"
    ollama_timeout: int = 300

    qdrant_host: str = "localhost"
    qdrant_port: int = 6333
    qdrant_collection: str = "documents"
    rag_top_k: int = 5

    fastapi_host: str = "0.0.0.0"
    fastapi_port: int = 8000

    log_level: str = "INFO"
    prefetch_count: int = 1

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"
        case_sensitive = False

settings = Settings()
