import httpx
import asyncio
from typing import List, Dict, Any, Optional
from datetime import datetime
from qdrant_client import QdrantClient
from qdrant_client.models import Filter, FieldCondition, MatchValue
from src.config import settings
from src.logger import logger

class OllamaService:

    def __init__(self, base_url: Optional[str]=None, model: Optional[str]=None, timeout: Optional[int]=None):
        self.base_url = (base_url or settings.ollama_base_url).rstrip('/')
        self.model = model or settings.ollama_model
        self.timeout = timeout or settings.ollama_timeout
        self.chat_endpoint = f'{self.base_url}/api/chat'
        logger.info(f'Initialized OllamaService: base_url={self.base_url}, model={self.model}, timeout={self.timeout}s')

    async def generate_response(self, prompt: str, conversation_history: Optional[List[Dict[str, str]]]=None, stream: bool=False) -> str:
        messages = conversation_history or []
        messages.append({'role': 'user', 'content': prompt})
        payload = {'model': self.model, 'messages': messages, 'stream': stream}
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
        self.client = QdrantClient(host=self.host, port=self.port)
        logger.info(f'Initialized QdrantService: {self.host}:{self.port}, collection={self.collection_name}')

    async def search_with_tenant_filter(self, query_vector: List[float], tenant_id: int, limit: int=5):
        search_filter = Filter(should=[FieldCondition(key='tenant_id', match=MatchValue(value=1)), FieldCondition(key='tenant_id', match=MatchValue(value=tenant_id))])
        results = self.client.search(collection_name=self.collection_name, query_vector=query_vector, query_filter=search_filter, limit=limit)
        logger.info(f'Qdrant search completed: tenant_id={tenant_id}, results={len(results)}')
        return results

    async def search_exact_tenant(self, query_vector: List[float], tenant_id: int, limit: int=1):
        search_filter = Filter(must=[FieldCondition(key='tenant_id', match=MatchValue(value=tenant_id))])
        results = self.client.search(collection_name=self.collection_name, query_vector=query_vector, query_filter=search_filter, limit=limit)
        logger.info(f'Qdrant exact search completed: tenant_id={tenant_id}, results={len(results)}')
        return results

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

class ChatBusiness:

    @staticmethod
    def _build_compliance_system_prompt() -> str:
        """
        Generates a comprehensive Vietnamese system prompt that enforces
        strict 3-step compliance reasoning process.
        """
        return """Bạn là trợ lý pháp lý chuyên nghiệp, có nhiệm vụ tư vấn về các quy định lao động và nội quy công ty.

**QUY TRÌNH TƯ VẤN BẮT BUỘC (3 BƯỚC):**

**BƯỚC 1 - ƯU TIÊN NỘI QUY CÔNG TY:**
- Bạn PHẢI ưu tiên tìm kiếm thông tin từ phần "**═══ NỘI QUY CÔNG TY ═══**" trước tiên.
- Nếu tìm thấy quy định liên quan trong Nội quy Công ty, đây là căn cứ CHÍNH để trả lời.
- Nội quy Công ty là quy định nội bộ áp dụng trực tiếp cho nhân viên.

**BƯỚC 2 - ĐỐI CHIẾU PHÁP LUẬT (KIỂM TRA TÍNH HỢP PHÁP):**
Sau khi tìm thấy thông tin trong Nội quy Công ty, bạn PHẢI kiểm tra phần "**═══ VĂN BẢN PHÁP LUẬT ═══**" để đối chiếu:

**Tình huống A - Nội quy có lợi hơn Pháp luật:**
- Nếu Nội quy Công ty quy định chế độ TốT HƠN so với Pháp luật (ví dụ: trả lương làm thêm giờ 160% thay vì 150% theo luật, hoặc nghỉ phép năm 15 ngày thay vì 12 ngày).
- Trả lời: Xác nhận quy định này HỢP LỆ và NHẤN MẠNH lợi ích vượt trội cho nhân viên.
- Ví dụ câu trả lời: "Theo Nội quy Công ty, mức lương làm thêm giờ là 160%, cao hơn mức 150% theo quy định của Bộ luật Lao động. Đây là chế độ ưu đãi của Công ty dành cho người lao động."

**Tình huống B - Nội quy vi phạm Pháp luật:**
- Nếu Nội quy Công ty quy định chế độ KÉM HƠN hoặc MÂU THUẪN với Pháp luật theo hướng BẤT LỢI cho người lao động (ví dụ: cho phép sa thải tùy ý, không trả trợ cấp thôi việc, giảm quyền lợi...).
- Trả lời: Nêu rõ quy định theo Nội quy, NHƯNG PHẢI CẢNH BÁO rằng quy định này có thể VI PHẠM Pháp luật Nhà nước.
- Ví dụ câu trả lời: "Theo Nội quy Công ty, quy định về [vấn đề X] là [nội dung quy định]. **⚠️ CẢNH BÁO PHÁP LÝ:** Quy định này có thể vi phạm Điều [số] Bộ luật Lao động, vì Pháp luật quy định [nội dung pháp luật]. Nhân viên có quyền khiếu nại hoặc tham khảo ý kiến luật sư lao động."

**Tình huống C - Nội quy phù hợp với Pháp luật:**
- Nếu Nội quy Công ty và Pháp luật quy định GIỐNG NHAU hoặc Nội quy tuân thủ đúng Pháp luật.
- Trả lời: Xác nhận Công ty đang áp dụng đúng theo quy định của Pháp luật.
- Ví dụ câu trả lời: "Quy định của Công ty về [vấn đề X] phù hợp với Điều [số] Bộ luật Lao động. Công ty đang tuân thủ đúng các quy định pháp luật hiện hành."

**BƯỚC 3 - CƠ CHẾ DỰ PHÒNG (KHI KHÔNG CÓ NỘI QUY):**
- Nếu KHÔNG tìm thấy thông tin trong phần "**═══ NỘI QUY CÔNG TY ═══**", hãy sử dụng phần "**═══ VĂN BẢN PHÁP LUẬT ═══**".
- Trả lời: BẮT BUỘC phải nêu rõ rằng Công ty chưa có quy định riêng, và đang áp dụng theo Pháp luật chung.
- Ví dụ câu trả lời: "Hiện tại Công ty chưa có quy định riêng về vấn đề này. Theo quy định chung của Pháp luật Nhà nước (Điều [số] Bộ luật Lao động), [nội dung pháp luật]. Nhân viên có thể tham khảo thêm tại [nguồn]."

**YÊU CẦU CHẤT LƯỢNG CÂU TRẢ LỜI:**
- Câu trả lời phải NGẮN GỌN, rõ ràng, khoảng 2-4 câu.
- Luôn trích dẫn CHÍNH XÁC nguồn gốc (Nội quy Công ty hay Văn bản Pháp luật).
- Với Tình huống B (vi phạm pháp luật), PHẢI có cảnh báo rõ ràng với ký hiệu "⚠️ CẢNH BÁO PHÁP LÝ".
- Với Tình huống A (có lợi hơn), PHẢI làm nổi bật lợi ích cho nhân viên.
- Không bịa đặt thông tin không có trong context.

**GHI NHỚ:**
Nội quy Công ty luôn được ưu tiên TRƯỚC, sau đó mới đối chiếu với Pháp luật để đảm bảo tính hợp pháp và công bằng cho người lao động."""

    @staticmethod
    def _structure_context_for_compliance(company_rule_results: list, legal_base_results: list, tenant_id: int) -> tuple[str, list, int]:
        """
        Structures the retrieved document chunks into a clear, delimited context string.

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

            # Step 1: Get embeddings and retrieve documents from both sources
            query_embedding = qdrant_service.get_embedding(message)

            # Parallel retrieval for efficiency
            legal_base_task = qdrant_service.search_exact_tenant(query_vector=query_embedding, tenant_id=1, limit=3)
            company_rule_task = qdrant_service.search_exact_tenant(query_vector=query_embedding, tenant_id=tenant_id, limit=3)
            (legal_base_results, company_rule_results) = await asyncio.gather(legal_base_task, company_rule_task, return_exceptions=True)

            # Handle exceptions
            if isinstance(legal_base_results, Exception):
                logger.error(f'[ConversationId: {conversation_id}] Legal base query failed: {legal_base_results}')
                legal_base_results = []
            if isinstance(company_rule_results, Exception):
                logger.error(f'[ConversationId: {conversation_id}] Company rule query failed: {company_rule_results}')
                company_rule_results = []

            # Step 2: Structure context with clear delimiters
            context_string, source_ids, documents_used = ChatBusiness._structure_context_for_compliance(
                company_rule_results=company_rule_results,
                legal_base_results=legal_base_results,
                tenant_id=tenant_id
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

            # Add compliance system prompt
            compliance_system_prompt = ChatBusiness._build_compliance_system_prompt()
            conversation_history.append({'role': 'system', 'content': compliance_system_prompt})
            logger.info(f'[ConversationId: {conversation_id}] Applied compliance system prompt')

            # Add dynamic system instructions if provided
            if system_instruction and len(system_instruction) > 0:
                system_prompt_parts = [item['value'] for item in system_instruction if 'value' in item]
                if system_prompt_parts:
                    dynamic_system_prompt = '\n\n'.join(system_prompt_parts)
                    conversation_history.append({'role': 'system', 'content': f"Hướng dẫn bổ sung:\n{dynamic_system_prompt}"})
                    logger.info(f'[ConversationId: {conversation_id}] Applied {len(system_prompt_parts)} additional system instruction(s)')

            # Step 5: Generate AI response
            ai_response = await ollama_service.generate_response(
                prompt=enhanced_prompt,
                conversation_history=conversation_history
            )
            logger.info(f'[ConversationId: {conversation_id}] Generated response (length: {len(ai_response)})')

            return {
                'conversation_id': conversation_id,
                'message': ai_response,
                'user_id': 0,
                'tenant_id': tenant_id,
                'timestamp': datetime.utcnow(),
                'model_used': ollama_service.model,
                'rag_documents_used': documents_used,
                'source_ids': source_ids
            }
        except Exception as e:
            logger.error(f'[ConversationId: {conversation_id}] Failed to process message: {e}', exc_info=True)
            raise