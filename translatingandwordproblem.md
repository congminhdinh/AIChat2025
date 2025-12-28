AIChat2025 => AI Chat 2025
shared infrastructure => lớp infrastructure dùng chung.
Separate Database per Tenant => ?
forward => gửi
user management => quản lý người dùng
tenant resolution logic => ?
centralize => ?
tenant isolation => ?
orchestrator => ?
\textit{ChatService} đóng vai trò orchestrator cho luồng hỏi đáp. Service này expose SignalR hub để WebApp kết nối, nhận messages từ users, persist messages vào SQL Server, và publish events lên RabbitMQ để ChatProcessor xử lý. ChatService cũng subscribe vào RabbitMQ để nhận bot responses và broadcast chúng về cho users qua SignalR. Việc sử dụng RabbitMQ giúp decouple ChatService khỏi ChatProcessor, cho phép ChatProcessor xử lý AI tasks một cách asynchronous mà không block ChatService. => ?
async I/O => ?
\textit{ChatProcessor} là AI worker phức tạp nhất, thực hiện toàn bộ RAG pipeline. Service này consume messages từ RabbitMQ, thực hiện query expansion, legal term extraction, gọi EmbeddingService để vectorize query, execute Hybrid Search (vector + BM25) song song trong cả tenant docs và global docs, apply RRF fusion, implement fallback logic, detect scenario, build context, và cuối cùng gọi LLM (Ollama + Vistral 7B) để generate response. ChatProcessor được thiết kế để xử lý concurrent requests hiệu quả nhờ Python async/await. => ?
asynchronous communication => ?
long-running AI tasks => ?
retry mechanism và guaranteed deliver =>?
resource utilization => ?
Separate Schema per Tenant => ?
maintain => ?
backup/restore operations => ?
data leakage => ?
subsequent requests => ?
access => ?
automatically append filter => ?
Tầng thứ tư là \textit{Database Layer}, nơi row-level security được enforce ở SQL level. Em đã implement query interceptor trong Entity Framework Core để tự động inject \texttt{WHERE TenantId = @currentTenantId} vào tất cả SELECT, UPDATE, DELETE queries. Ví dụ, query \texttt{SELECT * FROM Documents} tự động trở thành \texttt{SELECT * FROM Documents WHERE TenantId = 3}. Cơ chế này hoạt động ở mức ORM nên developers không thể accidentally query cross-tenant data ngay cả khi viết raw SQL.

Tầng thứ năm là \textit{Vector Database Layer} với Qdrant, nơi metadata filtering được apply. Mỗi vector trong Qdrant collection \texttt{vn\_law\_documents} có metadata field \texttt{tenant\_id}. Khi ChatProcessor search vectors, nó automatically include filter condition: \texttt{\{"must": [\{"key": "tenant\_id", "match": \{"value": 3\}\}]\}}. Điều này đảm bảo chỉ vectors thuộc tenant hiện tại được search, preventing information leakage qua vector similarity search.

Tầng thứ sáu là \textit{Storage Layer} với MinIO, nơi files được organize theo folder structure: \texttt{/tenant-\{tenantId\}/documents/\{filename\}}. StorageService enforce rule rằng mỗi tenant chỉ có thể read/write files trong folder của mình. Ví dụ, tenant 3 chỉ có thể access files trong \texttt{/tenant-3/} và không thể list hay download files từ \texttt{/tenant-2/} hay \texttt{/tenant-4/}. => ?
trust = >? 
legal domain => ?
exact citation => ?
abbreviation mismatch => ?
information retrieval => truy xuất thông tin
ChatProcessor thực hiện query expansion bằng cách thêm context từ system instruction, giúp query trở nên explicit hơn => ?
tạo ra ranking cuối cùng robust hơn => ?
internal policy => ?
Cuối cùng, Context Building step tổng hợp top-K chunks đã được ranked và filtered, kèm metadata (document name, article number, etc.), thành một context string. Context này cùng với query và scenario được gửi cho Ollama + Vistral 7B model để generate câu trả lời cuối cùng. Vistral 7B được chọn vì performance tốt với tiếng Việt và có thể chạy locally, đảm bảo data privacy. => ?
global docs => ?
implementation =>?