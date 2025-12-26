import logging
import httpx
from typing import List, Dict, Any, Optional
from qdrant_client import QdrantClient
from qdrant_client.models import Filter, FieldCondition, MatchValue, ScoredPoint
from app.config import settings
logger = logging.getLogger(__name__)

class QdrantService:

    def __init__(self, host: Optional[str]=None, port: Optional[int]=None, collection_name: Optional[str]=None):
        self.host = host or settings.qdrant_host
        self.port = port or settings.qdrant_port
        self.collection_name = collection_name or settings.qdrant_collection
        self.client = QdrantClient(host=self.host, port=self.port)
        logger.info(f'Initialized QdrantService: {self.host}:{self.port}, collection={self.collection_name}')

    async def search_with_tenant_filter(self, query_vector: List[float], tenant_id: int, limit: int=5) -> List[ScoredPoint]:
        # For Tenant 1 (Legal documents), we want to retrieve a mix of Law (type=1) and Decree (type=2)
        # For other tenants, we want to retrieve from BOTH Legal Law (tenant 1) AND Company Law (tenant X)

        if tenant_id == 1:
            # Search for tenant_id=1 to get both Law and Decree documents
            search_filter = Filter(must=[FieldCondition(key='tenant_id', match=MatchValue(value=1))])

            # Retrieve more results than needed for proper re-ranking by type
            search_limit = limit * 2
            results = self.client.search(collection_name=self.collection_name, query_vector=query_vector, query_filter=search_filter, limit=search_limit)

            # Separate results by type
            law_results = []
            decree_results = []

            for result in results:
                if hasattr(result, 'payload') and 'type' in result.payload:
                    doc_type = result.payload.get('type')
                    if doc_type == 1:  # Law (Luật)
                        law_results.append(result)
                    elif doc_type == 2:  # Decree (Nghị định)
                        decree_results.append(result)
                    else:
                        # Unknown type, add to law by default
                        law_results.append(result)
                else:
                    # No type field, add to law by default
                    law_results.append(result)

            # Combine: Law first, then Decree, respecting the limit
            prioritized_results = law_results + decree_results
            results = prioritized_results[:limit]

            logger.info(f'Qdrant search completed: tenant_id={tenant_id}, total={len(results)}, laws={len([r for r in results if r.payload.get("type")==1])}, decrees={len([r for r in results if r.payload.get("type")==2])}')

        else:
            # For other tenants: retrieve from BOTH company + legal with adaptive limits
            company_limit = max(1, int(limit * 0.6))  # 60% for company (e.g., 3 out of 5)
            legal_limit = max(1, limit - company_limit)  # 40% for legal (e.g., 2 out of 5)

            # Search 1: Company Law (priority)
            company_filter = Filter(must=[FieldCondition(key='tenant_id', match=MatchValue(value=tenant_id))])
            company_results = self.client.search(
                collection_name=self.collection_name,
                query_vector=query_vector,
                query_filter=company_filter,
                limit=company_limit
            )

            # Search 2: Legal Law (reference)
            legal_filter = Filter(must=[FieldCondition(key='tenant_id', match=MatchValue(value=1))])
            legal_results = self.client.search(
                collection_name=self.collection_name,
                query_vector=query_vector,
                query_filter=legal_filter,
                limit=legal_limit
            )

            # ADAPTIVE LOGIC: If one source empty, expand the other to fill gap
            company_count = len(company_results)
            legal_count = len(legal_results)

            if company_count == 0 and legal_count < limit:
                logger.warning(f'No company docs for tenant {tenant_id}, expanding legal from {legal_count} to {limit}')
                legal_results = self.client.search(
                    collection_name=self.collection_name,
                    query_vector=query_vector,
                    query_filter=legal_filter,
                    limit=limit  # Expand to full limit
                )
                legal_count = len(legal_results)

            elif legal_count == 0 and company_count < limit:
                logger.warning(f'No legal docs, expanding company from {company_count} to {limit}')
                company_results = self.client.search(
                    collection_name=self.collection_name,
                    query_vector=query_vector,
                    query_filter=company_filter,
                    limit=limit
                )
                company_count = len(company_results)

            # Combine: Company first, then Legal
            results = list(company_results) + list(legal_results)

            logger.info(f'Qdrant search: tenant_id={tenant_id}, total={len(results)}, company={company_count}, legal={legal_count}')

        return results

    def detect_scenario(self, company_results: List[ScoredPoint], legal_results: List[ScoredPoint]) -> str:
        """
        Detect retrieval scenario based on which sources returned results.

        Args:
            company_results: List of company regulation vectors found
            legal_results: List of legal base (Vietnam law) vectors found

        Returns:
            "BOTH": Both company regulation and legal base found
            "COMPANY_ONLY": Only company regulation found
            "LEGAL_ONLY": Only legal base found
            "NONE": No vectors found
        """
        has_company = len(company_results) > 0
        has_legal = len(legal_results) > 0

        if has_company and has_legal:
            return "BOTH"
        elif has_company and not has_legal:
            return "COMPANY_ONLY"
        elif not has_company and has_legal:
            return "LEGAL_ONLY"
        else:
            return "NONE"

    def get_embedding(self, text: str) -> List[float]:
        try:
            response = httpx.post(f'{settings.embedding_service_url}/embed', json={'text': text}, timeout=30.0)
            response.raise_for_status()
            result = response.json()
            return result['vector']
        except Exception as e:
            logger.error(f'Failed to get embedding from service: {e}')
            raise

    def health_check(self) -> bool:
        try:
            collections = self.client.get_collections()
            logger.info('Qdrant health check passed')
            return True
        except Exception as e:
            logger.error(f'Qdrant health check failed: {e}')
            return False