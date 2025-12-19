import httpx
import asyncio
from typing import List, Dict, Any, Optional
from datetime import datetime
from qdrant_client.async_qdrant_client import AsyncQdrantClient
from qdrant_client.models import Filter, FieldCondition, MatchValue, ScoredPoint
from src.config import settings
from src.logger import logger
from src.evaluation_logger import get_evaluation_logger

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
            SIMILARITY_THRESHOLD = 0.7
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
            SIMILARITY_THRESHOLD = 0.7
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

class ChatBusiness:

    @staticmethod
    def _detect_scenario(company_rule_results: list, legal_base_results: list) -> str:
        """
        Detect which scenario we're in based on vector retrieval results.

        Args:
            company_rule_results: List of company regulation vectors found
            legal_base_results: List of legal base (Vietnam law) vectors found

        Returns:
            "BOTH": Both company regulation and legal base found
            "COMPANY_ONLY": Only company regulation found
            "LEGAL_ONLY": Only legal base found
            "NONE": No vectors found
        """
        has_company = len(company_rule_results) > 0
        has_legal = len(legal_base_results) > 0

        if has_company and has_legal:
            return "BOTH"
        elif has_company and not has_legal:
            return "COMPANY_ONLY"
        elif not has_company and has_legal:
            return "LEGAL_ONLY"
        else:
            return "NONE"

    @staticmethod
    def _build_comparison_system_prompt() -> str:
        """
        Generates a comprehensive Vietnamese system prompt for COMPARISON mode.
        Used when BOTH company regulation and legal base vectors are found.
        Includes HARD CONSTRAINTS to prevent hallucination and verbosity.
        """
        return """Bạn là trợ lý pháp lý AI.
NHIỆM VỤ: Trả lời câu hỏi dựa trên việc đối chiếu giữa [NỘI QUY CÔNG TY] và [LUẬT NHÀ NƯỚC].

⛔ CÁC HARD CONSTRAINTS (BẮT BUỘC - KHÔNG ĐƯỢC VI PHẠM):
1. TUYỆT ĐỐI KHÔNG được in các cụm từ "Bước 1", "Bước 2", "Bước 3", "Step 1", "Step 2", hay bất kỳ hình thức mô tả quy trình suy luận nào.
2. TUYỆT ĐỐI KHÔNG được giải thích quá trình tư duy hoặc phân tích từng bước.
3. TUYỆT ĐỐI KHÔNG được trả lời dài dòng. Chỉ trả lời tối đa 2-3 câu ngắn gọn.
4. CHỈ trích xuất con số/phần trăm khớp CHÍNH XÁC với tình huống người dùng hỏi (ví dụ: nếu hỏi ca ngày thì lấy số liệu ca ngày, nếu hỏi ca đêm thì lấy số liệu ca đêm - KHÔNG được nhầm lẫn).
5. BẮT BUỘC phải so sánh chính xác giữa quy định Công ty và Luật Nhà nước.
6. TUYỆT ĐỐI KHÔNG cung cấp thông tin nếu thuộc danh mục tuyệt mật; phải đưa ra câu trả lời từ chối trực tiếp nếu nội dung yêu cầu vi phạm quy định bảo mật.

QUY ĐỊNH VỀ CẤU TRÚC CÂU TRẢ LỜI (BẮT BUỘC):
Bạn phải sử dụng chính xác khuôn mẫu câu dưới đây để trả lời, chỉ thay thế các phần trong ngoặc vuông `[...]` bằng thông tin thực tế tìm được trong văn bản:

"Theo [Trích dẫn Điều/Khoản Nội quy], công ty đang quy định [Số liệu/Quyền lợi của Công ty], và nó [Đánh giá: hợp lệ/cao hơn/thấp hơn] theo mức tối thiểu [Số liệu/Quyền lợi của Luật] quy định tại [Trích dẫn Điều/Khoản Luật]."

HƯỚNG DẪN ĐIỀN THÔNG TIN VÀO KHUÔN MẪU:
1. [Trích dẫn Điều/Khoản Nội quy]: Ghi rõ Điều số mấy trong Nội quy.
2. [Số liệu/Quyền lợi của Công ty]: Trích xuất con số hoặc quy định cụ thể của công ty (Ví dụ: số tiền, số %, số ngày...). ⚠️ CHÍNH XÁC với tình huống người dùng hỏi.
3. [Đánh giá]: So sánh và kết luận (dùng từ "hợp lệ", "cao hơn", hoặc "thấp hơn").
4. [Số liệu/Quyền lợi của Luật]: Trích xuất con số tương ứng trong Luật để làm mốc so sánh. ⚠️ CHÍNH XÁC với tình huống người dùng hỏi.
5. [Trích dẫn Điều/Khoản Luật]: Ghi rõ Điều khoản trong Luật Nhà nước.

YÊU CẦU:
- Không được tự ý thay đổi cấu trúc câu.
- Nếu nội quy công ty tốt hơn luật, hãy dùng từ "cao hơn" hoặc "tốt hơn".
- Trả lời ngắn gọn, dứt khoát, không giải thích vòng vo.
- CHỈ IN RA CÂU TRẢ LỜI CUỐI CÙNG. Không in bất kỳ tiêu đề hay tiền tố nào như "Trả lời:", "Câu trả lời:", "Câu trả lời cuối cùng:"."""

    @staticmethod
    def _build_single_source_system_prompt() -> str:
        """
        Generates a minimal Vietnamese system prompt for SINGLE SOURCE mode.
        Ultra-lightweight to reduce memory usage with vistral model.
        """
        return """TUYỆT ĐỐI KHÔNG in "Bước 1", "Bước 2", "Bước 3" hoặc bất kỳ quá trình suy luận nào.
CHỈ IN CÂU TRẢ LỜI CUỐI CÙNG.

Trả lời ngắn gọn theo mẫu: Theo [Điều X], [nội dung]. (Nguồn: [Nội quy Công ty/Luật Nhà nước])"""

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
        ]

        # Remove prefixes (case-insensitive)
        for prefix in prefixes_to_remove:
            if cleaned.lower().startswith(prefix.lower()):
                cleaned = cleaned[len(prefix):].strip()
                logger.debug(f'Removed prefix "{prefix}" from response')
                break  # Only remove the first matching prefix

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
        company_texts = [
            result.payload['text']
            for result in company_rule_results
            if hasattr(result, 'payload') and 'text' in result.payload
        ]

        if company_texts:
            context_parts.append("═══════════════════════════════════════")
            context_parts.append("**═══ NỘI QUY CÔNG TY ═══**")
            if scenario == "COMPANY_ONLY":
                context_parts.append("(Nguồn tài liệu duy nhất)")
            else:
                context_parts.append("(Quy định nội bộ - ưu tiên áp dụng)")
            context_parts.append("═══════════════════════════════════════")
            for idx, text in enumerate(company_texts, 1):
                context_parts.append(f"\n[Quy định #{idx}]\n{text}")

            # Collect source IDs
            for result in company_rule_results:
                if hasattr(result, 'payload') and 'source_id' in result.payload:
                    source_ids.append(result.payload['source_id'])

            documents_used += len(company_texts)
            logger.info(f'Retrieved {len(company_texts)} COMPANY REGULATION document(s) for tenant {tenant_id}')
        else:
            logger.warning(f'No COMPANY REGULATION documents found for tenant {tenant_id}')

        # Group B: Legal Framework (National Laws) - Reference/Validation source
        legal_texts = [
            result.payload['text']
            for result in legal_base_results
            if hasattr(result, 'payload') and 'text' in result.payload
        ]

        if legal_texts:
            context_parts.append("\n\n═══════════════════════════════════════")
            context_parts.append("**═══ VĂN BẢN PHÁP LUẬT ═══**")
            if scenario == "LEGAL_ONLY":
                context_parts.append("(Nguồn tài liệu duy nhất)")
            else:
                context_parts.append("(Quy định của Nhà nước - làm cơ sở đối chiếu)")
            context_parts.append("═══════════════════════════════════════")
            for idx, text in enumerate(legal_texts, 1):
                context_parts.append(f"\n[Văn bản #{idx}]\n{text}")

            # Collect source IDs
            for result in legal_base_results:
                if hasattr(result, 'payload') and 'source_id' in result.payload:
                    source_ids.append(result.payload['source_id'])

            documents_used += len(legal_texts)
            logger.info(f'Retrieved {len(legal_texts)} LEGAL FRAMEWORK document(s)')
        else:
            logger.warning('No LEGAL FRAMEWORK documents found')

        if not context_parts:
            return "", source_ids, documents_used

        # Join all parts with clear separation
        context_string = '\n'.join(context_parts)
        return context_string, source_ids, documents_used

    @staticmethod
    async def process_chat_message(conversation_id: int, user_id: int, message: str, tenant_id: int, ollama_service: OllamaService, qdrant_service: QdrantService, system_instruction: Optional[List[Dict[str, str]]]=None) -> Dict[str, Any]:
        try:
            logger.info(f"[ConversationId: {conversation_id}] Processing message from User {user_id}, Tenant {tenant_id}: '{message[:50]}...'")

            # Step 1: Query Expansion (Keyword Mapping)
            # Replace keys in raw_user_message with their corresponding values (descriptions)
            # to create enhanced_message with full semantic meaning
            enhanced_message = ChatBusiness._expand_query_with_prompt_config(message, system_instruction)
            logger.info(f"[ConversationId: {conversation_id}] Enhanced message: '{enhanced_message[:50]}...'")

            # Step 2: Embedding & Retrieval
            # CRITICAL: Use enhanced_message (NOT raw message) for vector search
            # to find documents based on full semantic meaning, not abbreviations
            query_embedding = await qdrant_service.get_embedding(enhanced_message)

            # Parallel retrieval for efficiency
            # Create coroutines (don't await yet) for true parallel execution
            legal_base_task = qdrant_service.search_exact_tenant(query_vector=query_embedding, tenant_id=1, limit=1)
            company_rule_task = qdrant_service.search_exact_tenant(query_vector=query_embedding, tenant_id=tenant_id, limit=1)
            (legal_base_results, company_rule_results) = await asyncio.gather(legal_base_task, company_rule_task, return_exceptions=True)

            # Handle exceptions
            if isinstance(legal_base_results, Exception):
                logger.error(f'[ConversationId: {conversation_id}] Legal base query failed: {legal_base_results}')
                legal_base_results = []
            if isinstance(company_rule_results, Exception):
                logger.error(f'[ConversationId: {conversation_id}] Company rule query failed: {company_rule_results}')
                company_rule_results = []

            # NEW: Detect scenario based on which vectors were found
            scenario = ChatBusiness._detect_scenario(company_rule_results, legal_base_results)
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
                    'scenario': scenario
                }

            # Step 2: Structure context with clear delimiters
            context_string, source_ids, documents_used = ChatBusiness._structure_context_for_compliance(
                company_rule_results=company_rule_results,
                legal_base_results=legal_base_results,
                tenant_id=tenant_id,
                scenario=scenario  # NEW: Pass scenario parameter
            )

            # Step 3: Build the enhanced prompt
            if context_string:
                enhanced_prompt = f"""Thông tin tham khảo:

{context_string}

Câu hỏi của người dùng: {message}

Hãy trả lời dựa trên thông tin được cung cấp ở trên, tuân thủ nghiêm ngặt quy trình 3 bước đã được hướng dẫn."""
            else:
                logger.warning(f'[ConversationId: {conversation_id}] No documents retrieved from either source. Using raw query.')
                enhanced_prompt = f"""Câu hỏi của người dùng: {message}

Lưu ý: Hiện không tìm thấy tài liệu tham khảo liên quan. Hãy trả lời dựa trên kiến thức chung về pháp luật lao động Việt Nam."""

            # Step 4: Build conversation history with system prompt
            conversation_history = []

            # NEW: Select system prompt based on scenario (BOTH or ONE)
            if scenario == "BOTH":
                compliance_system_prompt = ChatBusiness._build_comparison_system_prompt()
                logger.info(f'[ConversationId: {conversation_id}] Applied COMPARISON system prompt')
            else:  # scenario in ["COMPANY_ONLY", "LEGAL_ONLY"]
                compliance_system_prompt = ChatBusiness._build_single_source_system_prompt()
                logger.info(f'[ConversationId: {conversation_id}] Applied SINGLE SOURCE system prompt for {scenario}')

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
                'scenario': scenario  # NEW: Include scenario for debugging
            }
        except Exception as e:
            logger.error(f'[ConversationId: {conversation_id}] Failed to process message: {e}', exc_info=True)
            raise