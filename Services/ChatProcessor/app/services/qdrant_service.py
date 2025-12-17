import logging
import httpx
from typing import List, Dict, Any, Optional
from qdrant_client import QdrantClient
from qdrant_client.models import Filter, FieldCondition, MatchValue, ScoredPoint
from app.config import settings

logger = logging.getLogger(__name__)


class QdrantService:
    def __init__(
        self,
        host: Optional[str] = None,
        port: Optional[int] = None,
        collection_name: Optional[str] = None
    ):
        self.host = host or settings.qdrant_host
        self.port = port or settings.qdrant_port
        self.collection_name = collection_name or settings.qdrant_collection

        self.client = QdrantClient(host=self.host, port=self.port)

        logger.info(
            f"Initialized QdrantService: {self.host}:{self.port}, "
            f"collection={self.collection_name}"
        )

    async def search_with_tenant_filter(
        self,
        query_vector: List[float],
        tenant_id: int,
        limit: int = 5
    ) -> List[ScoredPoint]:
        search_filter = Filter(
            should=[
                FieldCondition(
                    key="tenant_id",
                    match=MatchValue(value=1)
                ),
                FieldCondition(
                    key="tenant_id",
                    match=MatchValue(value=tenant_id)
                )
            ]
        )

        results = self.client.search(
            collection_name=self.collection_name,
            query_vector=query_vector,
            query_filter=search_filter,
            limit=limit
        )

        logger.info(
            f"Qdrant search completed: tenant_id={tenant_id}, "
            f"results={len(results)}"
        )

        return results

    def get_embedding(self, text: str) -> List[float]:
        try:
            response = httpx.post(
                f"{settings.embedding_service_url}/embed",
                json={"text": text},
                timeout=30.0
            )
            response.raise_for_status()
            result = response.json()
            return result["vector"]
        except Exception as e:
            logger.error(f"Failed to get embedding from service: {e}")
            raise

    def health_check(self) -> bool:
        try:
            collections = self.client.get_collections()
            logger.info("Qdrant health check passed")
            return True
        except Exception as e:
            logger.error(f"Qdrant health check failed: {e}")
            return False
