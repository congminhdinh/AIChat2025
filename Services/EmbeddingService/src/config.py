import os
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    model_name: str = os.getenv('MODEL_NAME', 'truro7/vn-law-embedding')
    qdrant_host: str = os.getenv('QDRANT_HOST', 'localhost')
    qdrant_port: int = int(os.getenv('QDRANT_PORT', '6333'))
    qdrant_collection: str = os.getenv('QDRANT_COLLECTION', 'vn_law_documents')

    class Config:
        env_file = '.env'
settings = Settings()