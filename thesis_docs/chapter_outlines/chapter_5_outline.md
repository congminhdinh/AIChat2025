# CHƯƠNG 5: CÁC GIẢI PHÁP VÀ ĐÓNG GÓP NỔI BẬT

**Mục đích:** Trình bày các giải pháp sáng tạo và đóng góp kỹ thuật nổi bật của hệ thống.
**Số trang ước tính:** 10-12 trang.

**LƯU Ý:**
- Nội dung chương này được cấu trúc để giải quyết các vấn đề thực nghiệm đã nêu tại Chương 4.
- Các số liệu về độ chính xác và bảo mật hoàn toàn khớp với kết quả kiểm thử.

---

## DẪN NHẬP CHƯƠNG

Dựa trên cơ sở lý thuyết và các kết quả thực nghiệm từ Chương 4, chương này đi sâu phân tích 5 giải pháp kỹ thuật cốt lõi giúp hiện thực hóa hệ thống Multi-tenant RAG. Các giải pháp được trình bày theo tiến trình logic: từ mô hình hóa dữ liệu pháp luật, tối ưu thuật toán tìm kiếm để xử lý ngữ nghĩa tiếng Việt, đến kiến trúc nghiệp vụ so sánh luật và các giải pháp hạ tầng về bảo mật và hiệu năng.

---

## 5.1. Mô Hình Dữ Liệu Phân Cấp và Chunking Theo Ngữ Cảnh

> *Dẫn nhập: Vấn đề đầu tiên và nền tảng nhất cần giải quyết là sự đứt gãy ngữ cảnh khi máy tính đọc văn bản luật. Giải pháp mô hình hóa dữ liệu này tạo tiền đề bắt buộc để các thuật toán tìm kiếm và xử lý phía sau hoạt động chính xác.*

### 5.1.1. Vấn đề: Mất ngữ cảnh trong văn bản pháp quy
* **Đặc thù văn bản luật Việt Nam:** Tính phụ thuộc cao (Nghị định hướng dẫn Luật, Thông tư hướng dẫn Nghị định). RAG truyền thống cắt nhỏ văn bản (chunking) khiến các đoạn con mất liên kết với văn bản cha.
* **Minh họa thực tế:** Chunk *"Phạt tiền từ 5.000.000 đồng..."* trở nên vô nghĩa với LLM nếu không biết nó thuộc Nghị định nào, áp dụng cho hành vi gì (Hallucination).

### 5.1.2. Giải pháp: Self-Referencing Entity Model
* **Thiết kế dữ liệu tự tham chiếu:** Sử dụng cấu trúc quan hệ đệ quy (Recursive Relationship) trong Database: Luật (Root) $\to$ Nghị định (Child) $\to$ Thông tư (Grandchild).
* **Metadata Enrichment (Làm giàu ngữ cảnh):** Thay vì vector hóa text thô, hệ thống inject metadata vào từng chunk trước khi embedding.
    * *Format:* `[Tên Luật Cha] - [Tên Văn bản] - [Heading 1] - [Heading 2]: [Nội dung]`.

### 5.1.3. Hiệu quả đạt được
* **Khả năng trích dẫn ngược:** Hệ thống truy ngược được từ điều khoản cụ thể lên văn bản gốc.
* **Cơ sở cho hiển thị:** Hỗ trợ hiển thị trích dẫn phân cấp rõ ràng (như hình ảnh kết quả Chat trong Chương 4).

---

## 5.2. Tìm Kiếm Lai (Hybrid Search) Kết Hợp Vector và Từ Khóa

> *Dẫn nhập: Dữ liệu đã có cấu trúc tốt (Mục 5.1), nhưng thực nghiệm tại Chương 4 cho thấy độ chính xác tìm kiếm (Recall) ở các lĩnh vực chuyên sâu như An sinh chỉ đạt thấp do vấn đề "Semantic Gap". Mục này trình bày giải pháp kỹ thuật Hybrid Search nhằm khắc phục các hạn chế đó.*

### 5.2.1. Vấn đề: Khoảng cách ngữ nghĩa (Semantic Gap)
* **Phân tích thất bại từ thực nghiệm (Chương 4):**
    * *Từ viết tắt:* Query "BHXH" có vector xa so với text "Bảo hiểm xã hội".
    * *Từ đồng nghĩa:* Query đời thường ("trả lương ốm đau") lệch pha với thuật ngữ pháp lý ("trợ cấp ốm đau").
* **Hệ quả:** Tỷ lệ trả lời đúng (Pass rate) không đồng đều giữa các domain.

### 5.2.2. Giải pháp: Hybrid Search Pipeline
* **Legal Term Extractor (Trích xuất thuật ngữ):** Sử dụng Regex và từ điển Tenant để tự động mở rộng từ viết tắt (Query Expansion) trước khi tìm kiếm.
* **Cơ chế tìm kiếm song song:**
    * *Luồng 1 (Semantic):* Vector Search (Qdrant) để bắt ngữ nghĩa bao quát.
    * *Luồng 2 (Exact):* BM25 (Keyword) để bắt chính xác số hiệu điều luật (ví dụ: "Điều 24").
* **Reciprocal Rank Fusion (RRF):** Thuật toán gộp kết quả xếp hạng, ưu tiên các tài liệu xuất hiện ở cả 2 luồng.

### 5.2.3. Đánh giá hiệu quả cải tiến
* **Khắc phục điểm mù:** Giúp hệ thống tìm đúng các điều khoản cụ thể mà Vector search thuần túy thường bỏ sót.
* **Cải thiện độ chính xác:** Đây là giải pháp kỹ thuật chính để nâng cao tỷ lệ Pass rate trong các phiên bản tiếp theo.

---

## 5.3. Kiến Trúc Dual-RAG Hướng Tuân Thủ

> *Dẫn nhập: Sau khi đã tối ưu khả năng tìm kiếm thông tin (Mục 5.2), hệ thống áp dụng nó vào bài toán nghiệp vụ cốt lõi và phức tạp nhất của đồ án: Đối chiếu quy định nội bộ doanh nghiệp với pháp luật hiện hành.*

### 5.3.1. Vấn đề: Nhu cầu kiểm tra tuân thủ (Compliance Check)
* Doanh nghiệp không chỉ cần tra cứu, mà cần biết: *"Quy định của tôi có trái luật không?"*.
* Single-RAG (1 nguồn) không thể thực hiện so sánh chéo (Cross-reference).

### 5.3.2. Giải pháp: Kiến trúc Dual-Source Retrieval
* **Quy trình xử lý song song:**
    1.  *Query A* $\to$ Collection `Company_Policy` (Tenant ID = X).
    2.  *Query B* $\to$ Collection `National_Law` (Tenant ID = 1).
* **Context Fusion:** Ghép 2 đoạn văn bản vào Prompt theo cấu trúc so sánh đối xứng.
* **Prompt Engineering:** *"Dựa trên [Nội quy] và [Luật], hãy chỉ ra điểm mâu thuẫn..."*.

### 5.3.3. Kết quả thực tế (từ Pilot Deployment)
* **Phát hiện vi phạm:** Trong giai đoạn Pilot (Chương 4), hệ thống đã hỗ trợ phát hiện được 5 trường hợp nội quy trái luật (ví dụ: quy định thử việc quá thời gian).
* **Tính minh bạch:** Giao diện hiển thị rõ 2 nguồn trích dẫn với màu sắc phân biệt, hỗ trợ người dùng đối chiếu.

---

## 5.4. Truyền Ngữ Cảnh Tenant ở Tầng Infrastructure

> *Dẫn nhập: Để vận hành kiến trúc Dual-RAG (Mục 5.3) một cách an toàn cho nhiều doanh nghiệp cùng lúc, hệ thống bắt buộc phải có một cơ chế bảo mật cô lập dữ liệu tuyệt đối. Đây là điểm sáng nhất với kết quả kiểm thử đạt 100% tại Chương 4.*

### 5.4.1. Vấn đề: Rủi ro trong Multi-tenancy
* Mô hình Shared Database tiềm ẩn rủi ro lớn nhất là rò rỉ dữ liệu chéo (Cross-tenant leakage) do lỗi lập trình viên quên điều kiện lọc `WHERE`.

### 5.4.2. Giải pháp: Cô lập dữ liệu 6 tầng (Defense-in-Depth)
* **Nguyên lý:** Không tin tưởng vào code nghiệp vụ, áp dụng cô lập ngay từ hạ tầng (Infrastructure).
* **Các tầng bảo vệ:**
    1.  *Request Layer:* API Gateway xác thực JWT.
    2.  *Application Layer:* `ICurrentUserProvider` tự động lấy TenantId.
    3.  *Database Layer:* EF Core **Global Query Filter** tự động chèn `TenantId`.
    4.  *Vector DB Layer:* Qdrant Filter bắt buộc.
    5.  *Storage Layer:* Phân chia folder MinIO theo prefix.

### 5.4.3. Kết quả kiểm chứng (Khớp Chương 4)
* **Tỷ lệ an toàn tuyệt đối:** 8/8 test case bảo mật đều Đạt (100%).
* **Zero Leakage:** Không ghi nhận bất kỳ sự cố rò rỉ dữ liệu nào trong quá trình thực nghiệm.

---

## 5.5. Pipeline Xử Lý AI Bất Đồng Bộ Phân Tán

> *Dẫn nhập: Cuối cùng, để đảm bảo trải nghiệm người dùng mượt mà khi phải gánh chịu độ trễ xử lý AI trung bình 3.5 giây (như đo đạc ở Chương 4), hệ thống không thể dùng HTTP truyền thống mà chuyển sang kiến trúc xử lý bất đồng bộ.*

### 5.5.1. Vấn đề: Độ trễ và Tài nguyên
* Kết quả thực nghiệm cho thấy thời gian xử lý trung bình là 3.5s (Embedding + Search + Generation).
* HTTP Request đồng bộ sẽ gây block thread, lãng phí tài nguyên server và làm treo giao diện người dùng.

### 5.5.2. Giải pháp: Event-Driven Architecture
* **Mô hình RabbitMQ + SignalR:**
    1.  API nhận request $\to$ Đẩy vào Queue $\to$ Trả về `202 Accepted` ngay lập tức (Non-blocking).
    2.  Worker (Python) chạy ngầm tiêu thụ message.
    3.  Server đẩy kết quả xuống Client qua WebSocket (SignalR) khi hoàn tất.

### 5.5.3. Hiệu quả hệ thống
* **Trải nghiệm người dùng:** Giao diện phản hồi tức thì, có typing indicator, không bị timeout.
* **Độ ổn định:** Cơ chế Retry của RabbitMQ giúp hệ thống vận hành ổn định ngay cả khi tải cao trong giai đoạn Pilot.

---

## 5.6. Tổng kết chương

### Những đóng góp kỹ thuật chính:
1.  ✅ **Mô hình dữ liệu:** Giải quyết bài toán phân cấp luật.
2.  ✅ **Hybrid Search:** Đề xuất giải pháp cho vấn đề Semantic Gap.
3.  ✅ **Dual-RAG:** Hiện thực hóa kiểm tra tuân thủ tự động.
4.  ✅ **Bảo mật Multi-tenant:** Đạt độ an toàn 100% (kết quả thực tế).
5.  ✅ **Kiến trúc Bất đồng bộ:** Đảm bảo hiệu năng với độ trễ 3.5s.

### Chuyển tiếp sang Chương 6:
* Chương 5 đã phân tích sâu các giải pháp kỹ thuật.
* Chương 6 sẽ tổng kết toàn bộ đồ án, nhìn nhận thẳng thắn các hạn chế còn tồn tại (như tỷ lệ chính xác 46.8%) và đề xuất hướng phát triển.