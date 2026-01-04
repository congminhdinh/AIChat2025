import httpx
import asyncio
from typing import List, Dict, Any, Optional
from datetime import datetime
from qdrant_client.async_qdrant_client import AsyncQdrantClient
from qdrant_client.models import Filter, FieldCondition, MatchValue, MatchText, ScoredPoint
from src.config import settings
from src.logger import logger
from src.evaluation_logger import get_evaluation_logger
from src.hybrid_search import (
    LegalTermExtractor,
    ReciprocalRankFusion,
    HybridSearchStrategy,
    merge_and_deduplicate
)

class OllamaService:

    def __init__(self, base_url: Optional[str]=None, model: Optional[str]=None, timeout: Optional[int]=None):
        self.base_url = (base_url or settings.ollama_base_url).rstrip('/')
        self.model = model or settings.ollama_model
        self.timeout = timeout or settings.ollama_timeout
        self.chat_endpoint = f'{self.base_url}/api/chat'
        logger.info(f'Initialized OllamaService: base_url={self.base_url}, model={self.model}, timeout={self.timeout}s')

    async def generate_response(self, prompt: str, conversation_history: Optional[List[Dict[str, str]]]=None, stream: bool=False, temperature: Optional[float]=None) -> str:
        messages = conversation_history or []
        messages.append({'role': 'user', 'content': prompt})
        payload = {'model': self.model, 'messages': messages, 'stream': stream}

        # Inject temperature to reduce hallucination/creativity if provided
        if temperature is not None:
            payload['options'] = {'temperature': temperature}
            logger.debug(f'Setting temperature to {temperature} for reduced hallucination')

        logger.debug(f'Sending request to Ollama: {payload}')
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                response = await client.post(self.chat_endpoint, json=payload)
                response.raise_for_status()
                data = response.json()
                if 'message' in data and 'content' in data['message']:
                    ai_response = data['message']['content']
                    logger.info(f'Generated response (length: {len(ai_response)})')
                    return ai_response
                else:
                    logger.error(f'Unexpected response format: {data}')
                    raise ValueError(f'Unexpected response format: {data}')
        except httpx.TimeoutException as e:
            logger.error(f'Timeout calling Ollama: {e}')
            raise Exception(f'Ollama timeout after {self.timeout}s: {str(e)}')
        except httpx.HTTPStatusError as e:
            error_detail = 'Unknown error'
            try:
                error_body = e.response.text
                logger.error(f'HTTP error from Ollama: {e.response.status_code} | Response: {error_body}')
                error_detail = error_body[:200] if error_body else f'Status {e.response.status_code}'
            except:
                logger.error(f'HTTP error from Ollama: {e.response.status_code} (could not read response body)')
            raise Exception(f'Ollama error: {error_detail}')
        except httpx.RequestError as e:
            logger.error(f'Request error calling Ollama: {e}')
            raise Exception(f'Failed to connect to Ollama: {str(e)}')
        except Exception as e:
            logger.error(f'Error during AI generation: {e}', exc_info=True)
            raise

    async def list_models(self) -> list:
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                response = await client.get(f'{self.base_url}/api/tags')
                response.raise_for_status()
                data = response.json()
                models = [model.get('name', '') for model in data.get('models', [])]
                return models
        except Exception as e:
            logger.error(f'Failed to list Ollama models: {e}')
            return []

    async def health_check(self) -> bool:
        try:
            models = await self.list_models()
            if models:
                logger.info(f"Ollama health check passed. Available models: {', '.join(models)}")
                if self.model not in models:
                    logger.warning(f"Configured model '{self.model}' not found in Ollama. Available: {', '.join(models)}")
                    logger.warning(f'Please run: ollama pull {self.model}')
                return True
            else:
                logger.warning('Ollama is running but no models found. Please pull a model first.')
                return False
        except Exception as e:
            logger.error(f'Ollama health check failed: {e}')
            return False

class QdrantService:

    def __init__(self, host: Optional[str]=None, port: Optional[int]=None, collection_name: Optional[str]=None):
        self.host = host or settings.qdrant_host
        self.port = port or settings.qdrant_port
        self.collection_name = collection_name or settings.qdrant_collection
        self.client = AsyncQdrantClient(host=self.host, port=self.port)
        logger.info(f'Initialized QdrantService: {self.host}:{self.port}, collection={self.collection_name}')

    async def search_with_tenant_filter(
        self,
        query_vector: List[float],
        tenant_id: int,
        limit: int = 5
    ) -> List[ScoredPoint]:
        try:
            search_filter = Filter(
                should=[
                    FieldCondition(key='tenant_id', match=MatchValue(value=1)),
                    FieldCondition(key='tenant_id', match=MatchValue(value=tenant_id))
                ]
            )

            results = await self.client.search(
                collection_name=self.collection_name,
                query_vector=query_vector,
                query_filter=search_filter,
                limit=limit
            )

            # Filter by similarity score threshold
            SIMILARITY_THRESHOLD = 0.5
            filtered_results = [r for r in results if r.score >= SIMILARITY_THRESHOLD]

            # Log filtering activity
            if len(filtered_results) < len(results):
                logger.info(
                    f'Similarity filtering: {len(results)} -> {len(filtered_results)} results '
                    f'(excluded {len(results) - len(filtered_results)} below {SIMILARITY_THRESHOLD})'
                )

            logger.info(
                f'Qdrant search completed: tenant_id={tenant_id}, results={len(filtered_results)}'
            )
            return filtered_results

        except Exception as e:
            logger.error(
                f'Qdrant search failed for tenant_id={tenant_id}: {e}',
                exc_info=True
            )
            raise Exception(f'Vector search failed: {str(e)}')

    async def search_exact_tenant(
        self,
        query_vector: List[float],
        tenant_id: int,
        limit: int = 1
    ) -> List[ScoredPoint]:
        try:
            search_filter = Filter(
                must=[FieldCondition(key='tenant_id', match=MatchValue(value=tenant_id))]
            )

            results = await self.client.search(
                collection_name=self.collection_name,
                query_vector=query_vector,
                query_filter=search_filter,
                limit=limit
            )

            # Filter by similarity score threshold
            SIMILARITY_THRESHOLD = 0.5
            filtered_results = [r for r in results if r.score >= SIMILARITY_THRESHOLD]

            # Log filtering activity
            if len(filtered_results) < len(results):
                logger.info(
                    f'Similarity filtering: {len(results)} -> {len(filtered_results)} results '
                    f'(excluded {len(results) - len(filtered_results)} below {SIMILARITY_THRESHOLD})'
                )

            logger.info(
                f'Qdrant exact search completed: tenant_id={tenant_id}, results={len(filtered_results)}'
            )
            return filtered_results

        except Exception as e:
            logger.error(
                f'Qdrant search failed for tenant_id={tenant_id}: {e}',
                exc_info=True
            )
            raise Exception(f'Vector search failed: {str(e)}')

    async def get_embedding(self, text: str) -> List[float]:
        try:
            async with httpx.AsyncClient(timeout=30.0) as client:
                response = await client.post(
                    f'{settings.embedding_service_url}/embed',
                    json={'text': text}
                )
                response.raise_for_status()
                result = response.json()
                return result['vector']

        except httpx.TimeoutException as e:
            logger.error(f'Embedding service timeout: {e}')
            raise Exception(f'Embedding service timeout: {str(e)}')
        except httpx.HTTPStatusError as e:
            logger.error(f'Embedding service HTTP error: {e.response.status_code}')
            raise Exception(f'Embedding service error: {e.response.status_code}')
        except Exception as e:
            logger.error(f'Failed to get embedding from service: {e}', exc_info=True)
            raise

    async def health_check(self) -> bool:
        """
        Check if Qdrant service is healthy and accessible.

        Returns:
            True if health check passed, False otherwise
        """
        try:
            collections = await self.client.get_collections()
            logger.info('Qdrant health check passed')
            return True
        except Exception as e:
            logger.error(f'Qdrant health check failed: {e}')
            return False

    async def search_with_keywords(
        self,
        query_vector: List[float],
        keywords: List[str],
        tenant_id: int,
        limit: int = 10
    ) -> List[ScoredPoint]:
        """
        Hybrid search: combines vector similarity with keyword matching.

        Uses Qdrant's filter system to boost results containing specific keywords.
        Keywords are matched against: text, document_name, heading1, heading2.

        Args:
            query_vector: Embedding vector for semantic search
            keywords: List of keywords to match (e.g., ["điều 212", "BHXH"])
            tenant_id: Tenant ID for filtering
            limit: Maximum number of results

        Returns:
            List of ScoredPoint results ranked by hybrid score
        """
        try:
            # Build keyword filters - match any of the keywords in text or metadata
            keyword_conditions = []
            for keyword in keywords:
                # Match in text content
                keyword_conditions.append(
                    FieldCondition(
                        key='text',
                        match=MatchText(text=keyword)
                    )
                )
                # Match in document name
                keyword_conditions.append(
                    FieldCondition(
                        key='document_name',
                        match=MatchText(text=keyword)
                    )
                )
                # Match in headings
                keyword_conditions.append(
                    FieldCondition(
                        key='heading1',
                        match=MatchText(text=keyword)
                    )
                )
                keyword_conditions.append(
                    FieldCondition(
                        key='heading2',
                        match=MatchText(text=keyword)
                    )
                )

            # Build filter: must match tenant_id, should match keywords
            search_filter = Filter(
                must=[
                    FieldCondition(key='tenant_id', match=MatchValue(value=tenant_id))
                ],
                should=keyword_conditions if keyword_conditions else None
            )

            # Execute search with lower threshold for keyword matches
            KEYWORD_THRESHOLD = 0.6
            results = await self.client.search(
                collection_name=self.collection_name,
                query_vector=query_vector,
                query_filter=search_filter,
                limit=limit,
                score_threshold=KEYWORD_THRESHOLD
            )

            logger.info(
                f'Keyword search completed: tenant_id={tenant_id}, '
                f'keywords={keywords}, results={len(results)}'
            )

            return results

        except Exception as e:
            logger.error(
                f'Keyword search failed for tenant_id={tenant_id}: {e}',
                exc_info=True
            )
            # Return empty list on error to allow graceful degradation
            return []

    async def hybrid_search_single_tenant(
        self,
        query_vector: List[float],
        keywords: List[str],
        tenant_id: int,
        limit: int = 5
    ) -> List[ScoredPoint]:
        """
        Perform hybrid search for a single tenant using RRF fusion.

        Combines:
        1. Pure vector search (semantic similarity)
        2. Keyword-boosted search (exact term matching)
        3. RRF re-ranking

        Args:
            query_vector: Query embedding
            keywords: Extracted legal keywords
            tenant_id: Tenant to search in
            limit: Number of results to return

        Returns:
            Re-ranked results using RRF
        """
        try:
            # Run both searches in parallel
            vector_task = self.search_exact_tenant(
                query_vector=query_vector,
                tenant_id=tenant_id,
                limit=limit * 2  # Get more for better RRF
            )

            # Only run keyword search if we have keywords
            if keywords:
                keyword_task = self.search_with_keywords(
                    query_vector=query_vector,
                    keywords=keywords,
                    tenant_id=tenant_id,
                    limit=limit * 2
                )
                vector_results, keyword_results = await asyncio.gather(
                    vector_task, keyword_task, return_exceptions=True
                )

                # Handle exceptions
                if isinstance(vector_results, Exception):
                    logger.error(f'Vector search failed: {vector_results}')
                    vector_results = []
                if isinstance(keyword_results, Exception):
                    logger.error(f'Keyword search failed: {keyword_results}')
                    keyword_results = []

                # Fuse results using RRF
                fused_results = ReciprocalRankFusion.fuse(
                    vector_results=vector_results,
                    keyword_results=keyword_results,
                    k=60
                )

                logger.info(
                    f'Hybrid search for tenant {tenant_id}: '
                    f'{len(vector_results)} vector + {len(keyword_results)} keyword '
                    f'→ {len(fused_results)} fused'
                )

                return fused_results[:limit]
            else:
                # No keywords - fall back to pure vector search
                logger.debug(f'No keywords for tenant {tenant_id}, using vector search only')
                vector_results = await vector_task
                if isinstance(vector_results, Exception):
                    logger.error(f'Vector search failed: {vector_results}')
                    return []
                return vector_results

        except Exception as e:
            logger.error(
                f'Hybrid search failed for tenant_id={tenant_id}: {e}',
                exc_info=True
            )
            return []

    async def hybrid_search_with_fallback(
        self,
        query_vector: List[float],
        keywords: List[str],
        tenant_id: int,
        limit: int = 5
    ) -> tuple[List[ScoredPoint], List[ScoredPoint], bool]:
        """
        Perform hybrid search with intelligent fallback from tenant to global docs.

        Flow:
        1. Search tenant docs (hybrid: vector + keyword)
        2. Search global legal docs (hybrid: vector + keyword)
        3. Apply fallback logic based on tenant result quality

        Args:
            query_vector: Query embedding
            keywords: Extracted legal keywords
            tenant_id: Tenant ID
            limit: Total result limit

        Returns:
            (tenant_results, global_results, fallback_triggered)
        """
        try:
            # Parallel hybrid search in both scopes
            tenant_task = self.hybrid_search_single_tenant(
                query_vector=query_vector,
                keywords=keywords,
                tenant_id=tenant_id,
                limit=limit
            )
            global_task = self.hybrid_search_single_tenant(
                query_vector=query_vector,
                keywords=keywords,
                tenant_id=1,  # Global legal knowledge base
                limit=limit
            )

            tenant_results, global_results = await asyncio.gather(
                tenant_task, global_task, return_exceptions=True
            )

            # Handle exceptions
            if isinstance(tenant_results, Exception):
                logger.error(f'Tenant search failed: {tenant_results}')
                tenant_results = []
            if isinstance(global_results, Exception):
                logger.error(f'Global search failed: {global_results}')
                global_results = []

            # Apply fallback logic
            tenant_filtered, global_filtered, fallback = HybridSearchStrategy.apply_fallback_logic(
                tenant_results=tenant_results,
                global_results=global_results,
                limit=limit
            )

            if fallback:
                logger.warning(
                    f'Fallback activated for tenant {tenant_id}: '
                    f'{len(tenant_filtered)} tenant + {len(global_filtered)} global results'
                )
            else:
                logger.info(
                    f'Normal hybrid search for tenant {tenant_id}: '
                    f'{len(tenant_filtered)} tenant + {len(global_filtered)} global results'
                )

            return tenant_filtered, global_filtered, fallback

        except Exception as e:
            logger.error(f'Hybrid search with fallback failed: {e}', exc_info=True)
            return [], [], False

class ChatBusiness:

    @staticmethod
    def _detect_scenario(company_rule_results: list, legal_base_results: list, system_prompt: Optional[str] = None) -> str:
        """
        Detect which scenario we're in based on vector retrieval results and SystemPrompt availability.

        Args:
            company_rule_results: List of company regulation vectors found
            legal_base_results: List of legal base (Vietnam law) vectors found
            system_prompt: Optional SystemPrompt/Company Context for fallback

        Returns:
            "BOTH": Both company regulation and legal base found
            "COMPANY_ONLY": Only company regulation found
            "LEGAL_ONLY": Only legal base found
            "STATIC_CONTEXT": No RAG documents but SystemPrompt available
            "NONE": No vectors found and no SystemPrompt
        """
        has_company = len(company_rule_results) > 0
        has_legal = len(legal_base_results) > 0

        if has_company and has_legal:
            return "BOTH"
        elif has_company and not has_legal:
            return "COMPANY_ONLY"
        elif not has_company and has_legal:
            return "LEGAL_ONLY"
        elif not has_company and not has_legal:
            # NEW: Check for SystemPrompt fallback
            if system_prompt:
                return "STATIC_CONTEXT"
            else:
                return "NONE"
        else:
            return "NONE"

    @staticmethod
    def _build_comparison_system_prompt(fallback_mode: bool = False) -> str:
        """
        Generates a comprehensive Vietnamese system prompt for COMPARISON mode.
        Used when BOTH company regulation and legal base vectors are found.
        Includes HARD CONSTRAINTS to prevent hallucination and verbosity.

        Args:
            fallback_mode: If True, indicates that tenant docs were insufficient

        Returns:
            System prompt string
        """
        base_prompt = """Bạn là trợ lý pháp lý AI chuyên về so sánh quy định.

⛔ CẤM TUYỆT ĐỐI (VI PHẠM SẼ BỊ TỪ CHỐI):
- KHÔNG in "Bước 1", "Bước 2", "Bước 3", "Step 1", "Step 2" hay bất kỳ mô tả quy trình nào
- KHÔNG in các hướng dẫn như "Trích dẫn chính xác", "Trả lời câu hỏi", "Dựa trên ngữ cảnh"
- KHÔNG in tiền tố như "Trả lời:", "Câu trả lời:", "Kết luận:", "Dựa trên"
- KHÔNG giải thích quá trình tư duy hoặc phân tích
- KHÔNG trả lời dài dòng (chỉ tối đa 2-3 câu)
- KHÔNG nhầm lẫn số liệu (nếu hỏi ca ngày thì lấy ca ngày, hỏi ca đêm thì lấy ca đêm)
- KHÔNG cung cấp thông tin tuyệt mật

✓ ĐỊNH DẠNG ĐẦU RA (OUTPUT FORMAT):
Theo [Tên tài liệu nội quy - Điều X], công ty quy định [số liệu cụ thể], [đánh giá: hợp lệ/cao hơn/thấp hơn] mức tối thiểu [số liệu] quy định tại [Tên tài liệu luật - Điều Y].

📌 LƯU Ý QUAN TRỌNG:
- Sao chép CHÍNH XÁC tên tài liệu trong ngoặc vuông [...] từ phần "Thông tin tham khảo" bên dưới
- Trích xuất đúng số liệu (nếu hỏi ca ngày thì lấy ca ngày, hỏi ca đêm thì lấy ca đêm)
- Chỉ so sánh nội quy công ty với luật nhà nước
- Không giải thích, không dài dòng, chỉ 1-2 câu"""

        if fallback_mode:
            base_prompt += """\n\n⚠️ CHẾ ĐỘ FALLBACK:
Hệ thống đã tự động tìm kiếm trong cơ sở dữ liệu pháp luật chung do thiếu thông tin từ nội quy công ty.
Ưu tiên trích dẫn từ văn bản pháp luật Việt Nam."""

        return base_prompt

    @staticmethod
    def _build_single_source_system_prompt(fallback_mode: bool = False) -> str:
        """
        Generates a minimal Vietnamese system prompt for SINGLE SOURCE mode.
        Ultra-lightweight to reduce memory usage with vistral model.

        Args:
            fallback_mode: If True, indicates that tenant docs were insufficient

        Returns:
            System prompt string
        """
        base_prompt = """⛔ CẤM TUYỆT ĐỐI:
- KHÔNG in "Bước 1", "Bước 2", "Bước 3" hoặc bất kỳ quá trình suy luận nào
- KHÔNG in các hướng dẫn như "Trích dẫn chính xác từ ngữ cảnh", "Trả lời câu hỏi"
- KHÔNG in bất kỳ tiền tố nào như "Trả lời:", "Câu trả lời:", "Dựa trên"

✓ CHỈ IN CÂU TRẢ LỜI CUỐI CÙNG theo mẫu:
Theo [Tên tài liệu - Điều X], [nội dung cụ thể].

QUAN TRỌNG: Sao chép CHÍNH XÁC nhãn trong ngoặc vuông [...] từ "Thông tin tham khảo" bên dưới."""

        if fallback_mode:
            base_prompt += """\n\n⚠️ CHẾ ĐỘ FALLBACK: Hệ thống đã tự động tìm kiếm trong cơ sở dữ liệu pháp luật chung."""

        return base_prompt

    @staticmethod
    def _build_static_context_system_prompt() -> str:
        """
        Generates a system prompt for STATIC_CONTEXT mode.
        Used when no RAG documents are found but SystemPrompt/Company Context is available.

        This mode instructs the LLM to answer based solely on information in the SystemPrompt
        without document citations, since no RAG documents were retrieved.

        Returns:
            System prompt string for static context scenario
        """
        prompt = """Bạn là trợ lý AI thông minh.

⛔ CẤM TUYỆT ĐỐI:
- KHÔNG in "Bước 1", "Bước 2", "Bước 3" hoặc bất kỳ quá trình suy luận nào
- KHÔNG in các hướng dẫn như "Trích dẫn chính xác", "Trả lời câu hỏi"
- KHÔNG in bất kỳ tiền tố nào như "Trả lời:", "Câu trả lời:", "Dựa trên"
- KHÔNG trả lời dài dòng (chỉ tối đa 2-3 câu)

✓ HƯỚNG DẪN TRẢ LỜI:
- Trả lời dựa trên thông tin có sẵn trong phần giới thiệu về vai trò của bạn (SystemPrompt)
- Không cần trích dẫn tài liệu vì không có documents từ hệ thống RAG
- Chỉ sử dụng thông tin đã được cung cấp trong context, không bịa đặt
- Trả lời ngắn gọn, rõ ràng, tối đa 2-3 câu
- Nếu không có thông tin về câu hỏi, hãy thành thật nói "Tôi không có thông tin về vấn đề này"

📌 LƯU Ý:
Bạn đang trả lời dựa trên thông tin tĩnh (Static Context), không phải từ tài liệu được tìm kiếm."""

        return prompt

    @staticmethod
    def _cleanup_response(response: str) -> str:
        """
        Post-processing cleanup to remove Vietnamese prefixes and reasoning steps.
        Removes prefixes like "Trả lời:", "Câu trả lời:", and chain-of-thought reasoning (Bước 1, 2, 3...).
        """
        cleaned = response.strip()

        # Remove chain-of-thought reasoning steps (Bước 1, Bước 2, Bước 3, etc.)
        # Strategy: Extract only the content after the last "Bước X:" pattern
        import re

        # Find all "Bước" step markers
        steps = re.split(r'Bước \d+:', cleaned)

        if len(steps) > 1:
            # Take the last part (after the final "Bước X:")
            cleaned = steps[-1].strip()
            logger.debug(f'Removed {len(steps)-1} reasoning step(s) from response')

        # List of Vietnamese prefixes to remove (case-insensitive)
        prefixes_to_remove = [
            "Trả lời:",
            "Câu trả lời:",
            "Câu trả lời cuối cùng:",
            "Đáp án:",
            "Kết luận:",
            "Answer:",
            "Final answer:",
            "Xây dựng câu trả lời dựa trên các thông tin đã trích xuất.",
            "Dựa trên thông tin đã trích xuất,",
            "Trích dẫn chính xác từ ngữ cảnh và trả lời câu hỏi của người dùng.",
            "Trích dẫn chính xác từ ngữ cảnh.",
            "Trả lời câu hỏi của người dùng.",
            "Dựa trên ngữ cảnh,",
            "Dựa trên context,",
            "Căn cứ vào thông tin,",
            "Theo thông tin được cung cấp,",
        ]

        # Remove prefixes (case-insensitive) - loop multiple times in case of stacked prefixes
        max_iterations = 5  # Prevent infinite loop
        iteration = 0
        while iteration < max_iterations:
            removed = False
            for prefix in prefixes_to_remove:
                if cleaned.lower().startswith(prefix.lower()):
                    cleaned = cleaned[len(prefix):].strip()
                    logger.debug(f'Removed prefix "{prefix}" from response')
                    removed = True
                    break
            if not removed:
                break  # No more prefixes found
            iteration += 1

        # Remove instruction sentences that might appear as complete lines at the start
        # Pattern: If first line is an instruction and second line starts with "Theo", keep from second line
        lines = cleaned.split('\n')
        if len(lines) > 1:
            first_line_lower = lines[0].strip().lower()
            instruction_patterns = [
                'trích dẫn', 'trả lời', 'dựa trên', 'căn cứ', 'theo thông tin',
                'hãy', 'cần', 'phải', 'nên', 'vui lòng'
            ]
            if any(pattern in first_line_lower for pattern in instruction_patterns):
                if lines[1].strip().lower().startswith('theo'):
                    # First line looks like instruction, second line is the real answer
                    cleaned = '\n'.join(lines[1:]).strip()
                    logger.debug('Removed instruction sentence from first line')

        return cleaned

    @staticmethod
    def _expand_query_with_prompt_config(raw_message: str, prompt_config: Optional[List[Dict[str, str]]]) -> str:
        """
        Step 1: Query Expansion (Keyword Mapping)

        Replaces keys found in raw_message with their corresponding values (descriptions)
        from prompt_config to create an enhanced_message with full semantic meaning.

        Example: If config is {"OT": "Overtime Payment"} and user types "Calculate OT",
                 the enhanced_message will be "Calculate Overtime Payment"

        Args:
            raw_message: The original user message
            prompt_config: List of key-value pairs for terminology expansion

        Returns:
            enhanced_message: Message with keys replaced by their descriptions
        """
        if not prompt_config:
            logger.debug('No prompt_config provided, using raw message as-is')
            return raw_message

        enhanced_message = raw_message
        replacements_made = []

        # Iterate through each key-value pair in prompt_config
        for config_item in prompt_config:
            key = config_item.get('key', '')
            value = config_item.get('value', '')

            if not key or not value:
                continue

            # Check if key exists in the message (case-sensitive exact match)
            if key in enhanced_message:
                enhanced_message = enhanced_message.replace(key, value)
                replacements_made.append(f'"{key}" -> "{value}"')
                logger.debug(f'Replaced key "{key}" with "{value}"')

        if replacements_made:
            logger.info(f'Query expansion completed: {len(replacements_made)} replacement(s) made: {", ".join(replacements_made)}')
        else:
            logger.debug('No keys from prompt_config found in message')

        return enhanced_message

    @staticmethod
    def _build_terminology_definitions(prompt_config: Optional[List[Dict[str, str]]]) -> str:
        """
        Step 3: System Prompt Injection

        Builds a terminology definitions section from prompt_config to inject into system prompt.
        This ensures the LLM understands the specific terminology used in the retrieved context.

        Args:
            prompt_config: List of key-value pairs for terminology definitions

        Returns:
            A formatted string containing terminology definitions, or empty string if no config
        """
        if not prompt_config or len(prompt_config) == 0:
            return ""

        definitions = ["THUẬT NGỮ CHUYÊN MÔN (Terminology Definitions):"]
        for config_item in prompt_config:
            key = config_item.get('key', '')
            value = config_item.get('value', '')
            if key and value:
                definitions.append(f"- {key}: {value}")

        terminology_section = '\n'.join(definitions)
        logger.info(f'Built terminology definitions with {len(prompt_config)} term(s)')
        return terminology_section

    @staticmethod
    def _build_citation_label(result, is_company_rule: bool, index: int) -> str:
        """
        Builds a citation label from Qdrant result metadata.

        Args:
            result: Qdrant ScoredPoint with payload containing metadata
            is_company_rule: True for company rules, False for legal documents
            index: Fallback index number if metadata is missing

        Returns:
            Formatted citation label string
        """
        if not hasattr(result, 'payload'):
            return f"[Quy định #{index}]" if is_company_rule else f"[Văn bản #{index}]"

        payload = result.payload
        citation_parts = []

        # Extract metadata
        document_name = payload.get('document_name', '')
        heading1 = payload.get('heading1', '')
        heading2 = payload.get('heading2', '')

        # Build document identifier
        if document_name:
            citation_parts.append(document_name)
        else:
            # Fallback to generic label if no document name
            citation_parts.append(f"Quy định #{index}" if is_company_rule else f"Văn bản #{index}")

        # Add hierarchical section information
        if heading1 and heading2:
            citation_parts.append(f"{heading1} - {heading2}")
        elif heading1:
            citation_parts.append(heading1)
        elif heading2:
            citation_parts.append(heading2)

        # Join and wrap in brackets
        label = " - ".join(citation_parts)

        if is_company_rule and document_name:
            return f"[Nội quy: {label}]"
        else:
            return f"[{label}]"

    @staticmethod
    def _structure_context_for_compliance(company_rule_results: list, legal_base_results: list, tenant_id: int, scenario: str) -> tuple[str, list, int]:
        """
        Structures the retrieved document chunks into a clear, delimited context string.

        Args:
            company_rule_results: Company regulation vectors
            legal_base_results: Legal base vectors
            tenant_id: Tenant identifier
            scenario: One of "BOTH", "COMPANY_ONLY", "LEGAL_ONLY", or "NONE"

        Returns:
            tuple: (context_string, source_ids, documents_count)
        """
        context_parts = []
        source_ids = []
        documents_used = 0

        # Group A: Internal Policy (Company Rules) - Priority source
        # Extract company rule documents with metadata
        company_documents = [
            {
                'result': result,
                'text': result.payload['text'],
                'label': ChatBusiness._build_citation_label(result, is_company_rule=True, index=idx)
            }
            for idx, result in enumerate(company_rule_results, 1)
            if hasattr(result, 'payload') and 'text' in result.payload
        ]

        if company_documents:
            context_parts.append("═══════════════════════════════════════")
            context_parts.append("**═══ NỘI QUY CÔNG TY ═══**")
            if scenario == "COMPANY_ONLY":
                context_parts.append("(Nguồn tài liệu duy nhất)")
            else:
                context_parts.append("(Quy định nội bộ - ưu tiên áp dụng)")
            context_parts.append("═══════════════════════════════════════")
            for doc in company_documents:
                context_parts.append(f"\n{doc['label']}\n{doc['text']}")

            # Collect source IDs
            for result in company_rule_results:
                if hasattr(result, 'payload') and 'source_id' in result.payload:
                    source_ids.append(result.payload['source_id'])

            documents_used += len(company_documents)
            logger.info(f'Retrieved {len(company_documents)} COMPANY REGULATION document(s) for tenant {tenant_id}')
        else:
            logger.warning(f'No COMPANY REGULATION documents found for tenant {tenant_id}')

        # Group B: Legal Framework (National Laws) - Reference/Validation source
        # Extract legal documents with metadata
        legal_documents = [
            {
                'result': result,
                'text': result.payload['text'],
                'label': ChatBusiness._build_citation_label(result, is_company_rule=False, index=idx)
            }
            for idx, result in enumerate(legal_base_results, 1)
            if hasattr(result, 'payload') and 'text' in result.payload
        ]

        if legal_documents:
            context_parts.append("\n\n═══════════════════════════════════════")
            context_parts.append("**═══ VĂN BẢN PHÁP LUẬT ═══**")
            if scenario == "LEGAL_ONLY":
                context_parts.append("(Nguồn tài liệu duy nhất)")
            else:
                context_parts.append("(Quy định của Nhà nước - làm cơ sở đối chiếu)")
            context_parts.append("═══════════════════════════════════════")
            for doc in legal_documents:
                context_parts.append(f"\n{doc['label']}\n{doc['text']}")

            # Collect source IDs
            for result in legal_base_results:
                if hasattr(result, 'payload') and 'source_id' in result.payload:
                    source_ids.append(result.payload['source_id'])

            documents_used += len(legal_documents)
            logger.info(f'Retrieved {len(legal_documents)} LEGAL FRAMEWORK document(s)')
        else:
            logger.warning('No LEGAL FRAMEWORK documents found')

        if not context_parts:
            return "", source_ids, documents_used

        # Join all parts with clear separation
        context_string = '\n'.join(context_parts)
        return context_string, source_ids, documents_used

    @staticmethod
    async def process_chat_message(conversation_id: int, user_id: int, message: str, tenant_id: int, ollama_service: OllamaService, qdrant_service: QdrantService, system_instruction: Optional[List[Dict[str, str]]]=None, system_prompt: Optional[str]=None) -> Dict[str, Any]:
        try:
            logger.info(f"[ConversationId: {conversation_id}] Processing message from User {user_id}, Tenant {tenant_id}: '{message[:50]}...'")

            # Step 1: Query Expansion (Keyword Mapping)
            # Replace keys in raw_user_message with their corresponding values (descriptions)
            # to create enhanced_message with full semantic meaning
            enhanced_message = ChatBusiness._expand_query_with_prompt_config(message, system_instruction)
            logger.info(f"[ConversationId: {conversation_id}] Enhanced message: '{enhanced_message[:50]}...'")

            # Step 2: Legal Term Extraction & Keyword Preparation
            # Extract legal keywords from query for BM25 matching
            legal_keywords = LegalTermExtractor.extract_keywords(
                query=enhanced_message,
                system_instruction=system_instruction
            )
            logger.info(
                f'[ConversationId: {conversation_id}] Extracted {len(legal_keywords)} keywords: {legal_keywords}'
            )

            # Step 3: Embedding & Hybrid Retrieval with Fallback
            # CRITICAL: Use enhanced_message (NOT raw message) for vector search
            # to find documents based on full semantic meaning, not abbreviations
            query_embedding = await qdrant_service.get_embedding(enhanced_message)

            # Perform hybrid search with intelligent fallback
            company_rule_results, legal_base_results, fallback_triggered = await qdrant_service.hybrid_search_with_fallback(
                query_vector=query_embedding,
                keywords=legal_keywords,
                tenant_id=tenant_id,
                limit=5
            )

            logger.info(
                f'[ConversationId: {conversation_id}] Hybrid search completed: '
                f'{len(company_rule_results)} tenant + {len(legal_base_results)} global results '
                f'(fallback: {fallback_triggered})'
            )

            # NEW: Detect scenario based on which vectors were found and SystemPrompt availability
            scenario = ChatBusiness._detect_scenario(company_rule_results, legal_base_results, system_prompt)
            logger.info(f'[ConversationId: {conversation_id}] Detected scenario: {scenario}')

            # NEW: Handle NONE scenario - return error immediately without LLM generation
            if scenario == "NONE":
                logger.warning(f'[ConversationId: {conversation_id}] No vectors found, returning error response')
                timestamp = datetime.utcnow()
                return {
                    'conversation_id': conversation_id,
                    'message': 'Xin lỗi, hệ thống không tìm thấy thông tin chính xác',
                    'user_id': 0,
                    'tenant_id': tenant_id,
                    'timestamp': timestamp,
                    'model_used': ollama_service.model,
                    'rag_documents_used': 0,
                    'source_ids': [],
                    'reference_doc_id_list': [],  # NEW: Empty list for NONE scenario
                    'scenario': scenario
                }

            # Step 2: Structure context with clear delimiters (skip for STATIC_CONTEXT)
            if scenario == "STATIC_CONTEXT":
                # No RAG documents, only SystemPrompt - use simple prompt
                context_string = ""
                source_ids = []
                documents_used = 0
                logger.info(f'[ConversationId: {conversation_id}] STATIC_CONTEXT mode - skipping document structuring')
            else:
                context_string, source_ids, documents_used = ChatBusiness._structure_context_for_compliance(
                    company_rule_results=company_rule_results,
                    legal_base_results=legal_base_results,
                    tenant_id=tenant_id,
                    scenario=scenario  # NEW: Pass scenario parameter
                )

            # Step 3: Build the enhanced prompt
            if scenario == "STATIC_CONTEXT":
                # STATIC_CONTEXT: No document context, just the user question
                enhanced_prompt = f"""Câu hỏi của người dùng: {message}"""
                logger.info(f'[ConversationId: {conversation_id}] Built STATIC_CONTEXT prompt (no documents)')
            elif context_string:
                enhanced_prompt = f"""Thông tin tham khảo:

{context_string}

Câu hỏi của người dùng: {message}"""
            else:
                logger.warning(f'[ConversationId: {conversation_id}] No documents retrieved from either source. Using raw query.')
                enhanced_prompt = f"""Câu hỏi của người dùng: {message}

Lưu ý: Hiện không tìm thấy tài liệu tham khảo liên quan. Hãy trả lời dựa trên kiến thức chung về pháp luật lao động Việt Nam."""

            # Step 4: Build conversation history with system prompt
            conversation_history = []

            # Step 4.1: Tenant-specific behavioral instruction (SystemPrompt)
            # This comes FIRST to establish the overall persona/behavior
            if system_prompt:
                conversation_history.append({'role': 'system', 'content': system_prompt})
                logger.info(f'[ConversationId: {conversation_id}] Injected tenant-specific SystemPrompt')

            # Step 4.2: Select compliance system prompt based on scenario (BOTH, ONE, or STATIC_CONTEXT) and fallback status
            if scenario == "BOTH":
                compliance_system_prompt = ChatBusiness._build_comparison_system_prompt(
                    fallback_mode=fallback_triggered
                )
                logger.info(
                    f'[ConversationId: {conversation_id}] Applied COMPARISON system prompt '
                    f'(fallback: {fallback_triggered})'
                )
            elif scenario == "STATIC_CONTEXT":
                compliance_system_prompt = ChatBusiness._build_static_context_system_prompt()
                logger.info(
                    f'[ConversationId: {conversation_id}] Applied STATIC_CONTEXT system prompt '
                    f'(no RAG documents, using SystemPrompt only)'
                )
            else:  # scenario in ["COMPANY_ONLY", "LEGAL_ONLY"]
                compliance_system_prompt = ChatBusiness._build_single_source_system_prompt(
                    fallback_mode=fallback_triggered
                )
                logger.info(
                    f'[ConversationId: {conversation_id}] Applied SINGLE SOURCE system prompt '
                    f'for {scenario} (fallback: {fallback_triggered})'
                )

            conversation_history.append({'role': 'system', 'content': compliance_system_prompt})

            # Step 3: System Prompt Injection
            # Inject prompt_config definitions into system prompt so LLM understands the terminology
            terminology_definitions = ChatBusiness._build_terminology_definitions(system_instruction)
            if terminology_definitions:
                conversation_history.append({'role': 'system', 'content': terminology_definitions})
                logger.info(f'[ConversationId: {conversation_id}] Injected terminology definitions into system prompt')

            # Step 5: Generate AI response with temperature=0.1 to reduce hallucination
            ai_response = await ollama_service.generate_response(
                prompt=enhanced_prompt,
                conversation_history=conversation_history,
                temperature=0.1  # Low temperature to reduce creativity and hallucinations
            )
            logger.info(f'[ConversationId: {conversation_id}] Generated response (length: {len(ai_response)})')

            # Step 5.5: Post-processing cleanup to remove leaked prefixes
            ai_response = ChatBusiness._cleanup_response(ai_response)
            logger.info(f'[ConversationId: {conversation_id}] Response after cleanup (length: {len(ai_response)})')

            # Step 6: Extract contexts for evaluation logging
            contexts_list = []
            # Collect company rule texts
            for result in company_rule_results:
                if hasattr(result, 'payload') and 'text' in result.payload:
                    contexts_list.append(result.payload['text'])
            # Collect legal framework texts
            for result in legal_base_results:
                if hasattr(result, 'payload') and 'text' in result.payload:
                    contexts_list.append(result.payload['text'])

            # Step 7: Log evaluation metadata asynchronously (non-blocking)
            timestamp = datetime.utcnow()
            evaluation_logger = get_evaluation_logger()

            # Fire and forget - don't await to avoid blocking the response
            asyncio.create_task(
                evaluation_logger.log_interaction_async(
                    question=message,
                    contexts=contexts_list,
                    answer=ai_response,
                    conversation_id=conversation_id,
                    user_id=user_id,
                    tenant_id=tenant_id,
                    timestamp=timestamp
                )
            )
            logger.debug(f'[ConversationId: {conversation_id}] Scheduled evaluation metadata logging')

            return {
                'conversation_id': conversation_id,
                'message': ai_response,
                'user_id': 0,
                'tenant_id': tenant_id,
                'timestamp': timestamp,
                'model_used': ollama_service.model,
                'rag_documents_used': documents_used,
                'source_ids': source_ids,
                'reference_doc_id_list': source_ids,  # NEW: Also return as reference_doc_id_list for RabbitMQ event
                'scenario': scenario,  # NEW: Include scenario for debugging
                'fallback_triggered': fallback_triggered  # NEW: Include fallback status
            }
        except Exception as e:
            logger.error(f'[ConversationId: {conversation_id}] Failed to process message: {e}', exc_info=True)
            raise