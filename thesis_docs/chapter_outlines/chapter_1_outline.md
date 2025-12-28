# CHƯƠNG 1: GIỚI THIỆU

**Mục đích:** Giới thiệu bối cảnh, động lực, mục tiêu và phạm vi nghiên cứu của đề tài

**Số trang ước tính:** 8-10 trang

---

## 1.1. Giới thiệu đề tài

**Nội dung chính:**

### 1.1.1. Bối cảnh nghiên cứu
- Xu hướng chuyển đổi số trong doanh nghiệp Việt Nam
- Nhu cầu tư vấn pháp lý nội bộ trong các công ty lớn
- Thách thức: Nhân viên khó tiếp cận thông tin quy định nội bộ và văn bản pháp luật
- Hệ quả: Vi phạm quy định, rủi ro pháp lý, tốn thời gian tra cứu thủ công

### 1.1.2. Vấn đề thực tế
- **Hiện trạng:**
  - Văn bản pháp luật lao động Việt Nam phức tạp (Bộ luật Lao động 2019, 200+ điều luật)
  - Mỗi công ty có quy định nội bộ riêng (nội quy lao động, quy chế thưởng phạt, v.v.)
  - Nhân viên thường hỏi các câu hỏi lặp lại về phép, lương, chế độ, v.v.
  - Bộ phận nhân sự phải trả lời thủ công → tốn thời gian, không nhất quán

- **Giải pháp hiện tại:**
  - Hệ thống FAQ tĩnh (không linh hoạt)
  - Tài liệu PDF/Word (khó tìm kiếm, không ngữ cảnh)
  - Email/chat trực tiếp với HR (chậm, không mở rộng)

- **Hạn chế:**
  - Không trả lời câu hỏi phức tạp, đa ngữ cảnh
  - Không kết hợp quy định công ty + luật nhà nước
  - Không phù hợp với tiếng Việt (các giải pháp nước ngoài)

### 1.1.3. Giải pháp đề xuất
- Xây dựng hệ thống chatbot tư vấn pháp lý nội bộ sử dụng **RAG (Retrieval-Augmented Generation)**
- Kết hợp **tìm kiếm vector (Qdrant)** + **LLM tiếng Việt (Vistral)** + **mô hình embedding chuyên biệt (vn-law-embedding)**
- Hỗ trợ **đa tenant** (multi-tenant): một hệ thống phục vụ nhiều công ty, cô lập dữ liệu
- Tích hợp **quy định công ty** và **văn bản pháp luật nhà nước**, trả lời dựa trên cả hai nguồn

**Sơ đồ sử dụng:**
- Tham khảo: `thesis_docs/diagrams_to_create.md` → Diagram 1.1 (Use Case Overview)
- Mô tả: Nhân viên hỏi chatbot → Hệ thống tìm kiếm trong cơ sở tri thức → Trả lời với trích dẫn cụ thể

---

## 1.2. Lý do chọn đề tài

**Nội dung chính:**

### 1.2.1. Tính cấp thiết
- **Thực tế doanh nghiệp:**
  - Công ty 500+ nhân viên có hàng chục câu hỏi HR/ngày
  - Bộ phận nhân sự 3-5 người không đủ thời gian tư vấn
  - Cần giải pháp tự động hóa để giảm tải

- **Xu hướng công nghệ:**
  - RAG là xu hướng mới nhất trong AI (2023-2024)
  - LLM tiếng Việt đã có chất lượng cao (Vistral, PhoGPT)
  - Cơ hội nghiên cứu ứng dụng thực tế tại Việt Nam

### 1.2.2. Tính sáng tạo
- **Điểm mới:**
  - Kết hợp **dual-RAG**: tìm kiếm song song trong quy định công ty + luật nhà nước
  - Thuật toán **so sánh quy định công ty với pháp luật** để đảm bảo tuân thủ
  - Xử lý **văn bản pháp luật Việt Nam có cấu trúc phân cấp** (Chương/Mục/Điều/Khoản)
  - Kiến trúc **multi-tenant** với cô lập dữ liệu ở mức row-level

- **Khác biệt với các nghiên cứu trước:**
  - Các hệ thống RAG quốc tế không phù hợp với văn bản pháp luật Việt Nam
  - Các chatbot pháp lý Việt Nam hiện tại không hỗ trợ quy định nội bộ công ty
  - Chưa có hệ thống nào kết hợp multi-tenancy + dual-RAG + văn bản phân cấp

### 1.2.3. Ý nghĩa cá nhân
- Kết hợp kiến thức về **AI/Machine Learning** + **Software Engineering** + **Domain Knowledge (pháp luật lao động)**
- Cơ hội làm việc với công nghệ tiên tiến: LLM, vector database, microservices
- Giải quyết vấn đề thực tế có thể thương mại hóa sau tốt nghiệp

---

## 1.3. Mục tiêu nghiên cứu

**Nội dung chính:**

### 1.3.1. Mục tiêu tổng quát
Xây dựng hệ thống chatbot tư vấn pháp lý nội bộ sử dụng RAG, hỗ trợ đa công ty (multi-tenant), có khả năng:
1. Trả lời câu hỏi về pháp luật lao động Việt Nam
2. Trả lời câu hỏi về quy định nội bộ công ty
3. So sánh và phát hiện mâu thuẫn giữa quy định công ty và luật nhà nước
4. Cung cấp trích dẫn chính xác (tên văn bản, điều khoản)

### 1.3.2. Mục tiêu cụ thể

**Về kỹ thuật:**
1. **Xây dựng pipeline RAG** cho văn bản pháp luật tiếng Việt
   - Hierarchical semantic chunking (bảo toàn cấu trúc Chương/Điều)
   - Embedding với mô hình chuyên biệt (vn-law-embedding, 768-dim)
   - Vector search với Qdrant (COSINE distance)
   - LLM generation với Vistral (Vietnamese-finetuned)

2. **Thiết kế kiến trúc multi-tenant**
   - Row-level security với TenantId filtering
   - Cô lập dữ liệu tuyệt đối giữa các công ty
   - Shared database pattern (tối ưu chi phí)

3. **Xây dựng dual-RAG**
   - Tìm kiếm song song trong 2 collection: company rules + legal base
   - Thuật toán kết hợp kết quả (prioritization, comparison)
   - Prompt engineering cho 3 scenario: company-only, legal-only, comparison

4. **Phát triển microservices**
   - 9 microservices (.NET + Python) giao tiếp qua RabbitMQ, SignalR
   - API Gateway (YARP) cho routing
   - Background jobs (Hangfire) cho vectorization

**Về nghiên cứu:**
5. **Đánh giá chất lượng RAG**
   - Sử dụng RAGAS framework (Faithfulness, Answer Relevancy, Context Recall, Context Precision)
   - So sánh kết quả RAG vs Non-RAG
   - Phân tích độ chính xác trích dẫn

6. **Phân tích hiệu năng**
   - Đo latency của RAG pipeline (embedding, search, generation)
   - Đo API response time
   - Đo resource usage (CPU, RAM, disk)

**Về sản phẩm:**
7. **Xây dựng ứng dụng web hoàn chỉnh**
   - Frontend: ASP.NET MVC + Razor + SignalR Client
   - Backend: 7 microservices .NET + 2 AI workers Python
   - Infrastructure: Docker Compose với 13 containers

---

## 1.4. Phạm vi nghiên cứu

**Nội dung chính:**

### 1.4.1. Phạm vi chức năng

**Bao gồm:**
- ✅ Quản lý tài khoản (đăng ký, đăng nhập, JWT authentication)
- ✅ Quản lý tenant (tạo công ty, phân quyền)
- ✅ Quản lý tài liệu (upload .docx, vectorization tự động)
- ✅ Tìm kiếm vector (Qdrant) và RAG pipeline
- ✅ Chat real-time (SignalR WebSocket)
- ✅ Message queue (RabbitMQ) cho xử lý bất đồng bộ
- ✅ Admin dashboard (Hangfire, Swagger)

**Không bao gồm:**
- ❌ Unit tests, integration tests (documented as future work)
- ❌ Advanced features: message editing, conversation export, voice input
- ❌ Production hardening: caching (Redis), monitoring (Grafana), CI/CD
- ❌ Mobile app (chỉ có web app)
- ❌ Email notifications

### 1.4.2. Phạm vi kỹ thuật

**Công nghệ sử dụng:**
- Backend: .NET 9, Python 3.11+
- AI: HuggingFace Transformers, Ollama, RAGAS
- Database: SQL Server 2022, Qdrant (vector DB)
- Message Queue: RabbitMQ 3
- Storage: MinIO (S3-compatible)
- Deployment: Docker Compose

**Chi tiết công nghệ:** Xem `thesis_docs/technology_inventory.md`

### 1.4.3. Phạm vi dữ liệu

**Nguồn dữ liệu:**
- **Pháp luật nhà nước:** Bộ luật Lao động 2019 (Law No. 45/2019/QH14)
  - 200+ điều luật
  - Cấu trúc: Chương → Mục → Điều → Khoản
  - Định dạng: .docx (Official Government Portal)

- **Quy định công ty:** Mẫu nội quy lao động (demo)
  - Nội quy lao động, quy chế thưởng phạt, quy định nghỉ phép
  - Cấu trúc: Chương → Điều → Khoản
  - Định dạng: .docx

**Không bao gồm:**
- Nghị định, thông tư (có thể mở rộng)
- Pháp luật ngoài lao động (dân sự, hình sự, v.v.)
- Văn bản tiếng Anh

### 1.4.4. Phạm vi đối tượng sử dụng

**Người dùng mục tiêu:**
- Nhân viên văn phòng (office workers)
- Bộ phận nhân sự (HR department)
- Quản trị viên hệ thống (system admin)

**Quy mô:**
- Công ty vừa và lớn (100-1000+ nhân viên)
- Hỗ trợ đa tenant (không giới hạn số công ty)

---

## 1.5. Phương pháp nghiên cứu

**Nội dung chính:**

### 1.5.1. Quy trình nghiên cứu

**Bước 1: Nghiên cứu lý thuyết (Literature Review)**
- Khảo sát các nghiên cứu về RAG, LLM, multi-tenancy
- Tìm hiểu các hệ thống chatbot pháp lý hiện có
- Phân tích đặc thù văn bản pháp luật Việt Nam
- **Thời gian:** 2 tuần
- **Kết quả:** Chương 2 của luận văn

**Bước 2: Phân tích và thiết kế hệ thống (Analysis & Design)**
- Xác định yêu cầu chức năng, phi chức năng
- Thiết kế kiến trúc microservices
- Thiết kế cơ sở dữ liệu (SQL + vector DB)
- Thiết kế RAG pipeline
- **Thời gian:** 2 tuần
- **Kết quả:** Use case diagram, ER diagram, C4 architecture diagram

**Bước 3: Lựa chọn công nghệ (Technology Selection)**
- So sánh các LLM tiếng Việt (Vistral, PhoGPT, Gemma-Vietnamese)
- So sánh các mô hình embedding (vn-law-embedding, PhoBERT, multilingual-e5)
- So sánh vector database (Qdrant, Milvus, Weaviate)
- **Thời gian:** 1 tuần
- **Kết quả:** Technology inventory (Chương 3)

**Bước 4: Triển khai hệ thống (Implementation)**
- Xây dựng 9 microservices
- Triển khai RAG pipeline
- Phát triển giao diện web
- Tích hợp các thành phần
- **Thời gian:** 8 tuần
- **Kết quả:** Source code (25,000+ LOC)

**Bước 5: Thử nghiệm và đánh giá (Testing & Evaluation)**
- Test thủ công các chức năng
- Đánh giá chất lượng RAG với RAGAS
- Đo hiệu năng (latency, throughput)
- Thu thập phản hồi người dùng (nếu có)
- **Thời gian:** 2 tuần
- **Kết quả:** Chương 5

**Bước 6: Viết luận văn và chuẩn bị bảo vệ**
- Viết báo cáo
- Tạo slide thuyết trình
- Quay video demo
- **Thời gian:** 2 tuần

**Tổng thời gian:** 17 tuần (~4 tháng)

**Sơ đồ quy trình:**
- Tham khảo: `thesis_docs/diagrams_to_create.md` → Diagram 1.3 (Research Methodology Flowchart)

### 1.5.2. Phương pháp thử nghiệm

**1. Functional Testing (Thử nghiệm chức năng)**
- Manual testing cho các use case chính
- Không có unit tests, integration tests (documented as future work)

**2. RAG Evaluation (Đánh giá RAG)**
- **Framework:** RAGAS (Retrieval-Augmented Generation Assessment)
- **Metrics:**
  - **Faithfulness:** Câu trả lời có trung thực với context không?
  - **Answer Relevancy:** Câu trả lời có liên quan đến câu hỏi không?
  - **Context Recall:** Hệ thống có tìm được tất cả context liên quan không?
  - **Context Precision:** Context có chứa nhiều thông tin nhiễu không?
- **Dataset:** 20-30 câu hỏi mẫu về lao động
- **Comparison:** RAG vs Non-RAG (LLM knowledge only)

**3. Performance Testing (Đánh giá hiệu năng)**
- **API Response Time:** Đo thời gian từ request đến response
- **RAG Pipeline Latency:** Đo từng bước (embedding → search → generation)
- **Resource Usage:** CPU, RAM, disk (Docker stats)
- **Tools:** Serilog logging, manual measurement

**4. Usability Testing (Đánh giá trải nghiệm người dùng)**
- Demo cho 3-5 người dùng thử
- Thu thập phản hồi định tính
- Không có formal usability study

### 1.5.3. Phương pháp phân tích dữ liệu

**1. Code Statistics**
- Đếm số file, lines of code, số class, số API endpoint
- **Tool:** find, wc, grep (manual count)
- **Output:** `thesis_docs/code_statistics.json`

**2. RAG Metrics**
- RAGAS framework tự động tính toán
- Visualize bằng matplotlib/seaborn
- So sánh trước/sau cải tiến

**3. Performance Metrics**
- Log analysis với Serilog
- Tính trung bình, p95, p99 latency
- Plot charts với Excel/Python

---

## 1.6. Ý nghĩa khoa học và thực tiễn

**Nội dung chính:**

### 1.6.1. Ý nghĩa khoa học

**1. Đóng góp về mặt kỹ thuật:**
- Đề xuất **hierarchical semantic chunking** cho văn bản pháp luật Việt Nam có cấu trúc phân cấp
- Thiết kế **dual-RAG architecture** kết hợp hai nguồn tri thức (company rules + legal base)
- Giải pháp **multi-tenant row-level security** cho hệ thống RAG

**2. Đóng góp về mặt nghiên cứu:**
- Nghiên cứu điển hình về áp dụng RAG cho domain-specific (pháp luật lao động Việt Nam)
- Đánh giá hiệu quả LLM tiếng Việt (Vistral) trong bài toán tư vấn pháp lý
- Benchmark RAGAS metrics cho RAG system tiếng Việt

**3. Tài liệu tham khảo:**
- Source code hoàn chỉnh (25,000+ LOC) có thể tái sử dụng
- System analysis report (25,000+ words) là tài liệu tham khảo cho sinh viên khác
- PlantUML diagrams có thể dùng để giảng dạy kiến trúc microservices

### 1.6.2. Ý nghĩa thực tiễn

**1. Giải quyết vấn đề thực tế:**
- Giảm tải công việc tư vấn cho bộ phận nhân sự
- Nhân viên có thể tự tra cứu thông tin 24/7 (không cần chờ HR)
- Đảm bảo tính nhất quán trong tư vấn (không phụ thuộc vào kiến thức cá nhân)

**2. Tiềm năng thương mại hóa:**
- Sản phẩm có thể triển khai thực tế cho doanh nghiệp
- Mô hình SaaS multi-tenant giảm chi phí triển khai
- Có thể mở rộng cho các lĩnh vực khác (tài chính, y tế, giáo dục)

**3. Tác động xã hội:**
- Nâng cao nhận thức pháp luật của người lao động
- Giảm thiểu vi phạm quy định do thiếu thông tin
- Hỗ trợ doanh nghiệp tuân thủ pháp luật lao động

---

## 1.7. Cấu trúc luận văn

**Nội dung chính:**

Luận văn gồm 6 chương và 2 phụ lục:

### **CHƯƠNG 1: GIỚI THIỆU**
- Giới thiệu đề tài, lý do chọn đề tài
- Mục tiêu, phạm vi nghiên cứu
- Phương pháp nghiên cứu
- Ý nghĩa khoa học và thực tiễn
- **Số trang:** 8-10 trang

### **CHƯƠNG 2: KHẢO SÁT VÀ PHÂN TÍCH**
- Tổng quan về RAG, LLM, multi-tenancy
- Các nghiên cứu liên quan
- Phân tích yêu cầu hệ thống
- **Số trang:** 15-18 trang

### **CHƯƠNG 3: CÁC CÔNG NGHỆ SỬ DỤNG**
- Tổng quan về công nghệ
- Backend: .NET 9, ASP.NET Core, Entity Framework Core
- AI: HuggingFace Transformers, Ollama, RAGAS
- Database: SQL Server, Qdrant
- Infrastructure: Docker, RabbitMQ, MinIO
- **Số trang:** 12-15 trang

### **CHƯƠNG 4: THIẾT KẾ VÀ TRIỂN KHAI**
- Thiết kế kiến trúc hệ thống (C4 model)
- Thiết kế cơ sở dữ liệu
- Thiết kế RAG pipeline
- Triển khai các microservices
- Triển khai frontend
- **Số trang:** 30-35 trang

### **CHƯƠNG 5: KẾT QUẢ VÀ ĐÁNH GIÁ**
- Kết quả triển khai
- Đánh giá chất lượng RAG (RAGAS metrics)
- Đánh giá hiệu năng
- Phân tích ưu/nhược điểm
- **Số trang:** 12-15 trang
- **Lưu ý:** Chương này đã được hoàn thành, các chương khác chỉ tham chiếu

### **CHƯƠNG 6: KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN**
- Tổng kết những gì đã làm được
- Hạn chế của hệ thống
- Hướng phát triển trong tương lai
- **Số trang:** 5-7 trang

### **PHỤ LỤC A: SOURCE CODE**
- Link GitHub repository
- Hướng dẫn cài đặt
- **Số trang:** 2-3 trang

### **PHỤ LỤC B: ĐẶC TẢ USE CASE CHI TIẾT**
- Mô tả chi tiết các use case
- Use case diagram
- Activity diagram
- **Số trang:** 8-10 trang

**Tổng số trang ước tính:** 92-113 trang (không kể tài liệu tham khảo)

---

## TÀI LIỆU THAM KHẢO CHO CHƯƠNG 1

### Sách và tài liệu học thuật
1. **Software Engineering:** Ian Sommerville (2016) - Software Engineering, 10th Edition
2. **Microservices:** Chris Richardson (2018) - Microservices Patterns
3. **RAG:** Lewis et al. (2020) - "Retrieval-Augmented Generation for Knowledge-Intensive NLP Tasks"
4. **Multi-tenancy:** Guo et al. (2007) - "A Framework for Native Multi-Tenancy Application Development and Management"

### Tài liệu kỹ thuật
5. .NET 9 Documentation: https://learn.microsoft.com/dotnet/
6. RAGAS Documentation: https://docs.ragas.io/
7. Qdrant Documentation: https://qdrant.tech/documentation/

### Văn bản pháp luật
8. Bộ luật Lao động 2019 (Luật số 45/2019/QH14)

---

**KẾT THÚC CHƯƠNG 1**

**Điểm nhấn chính:**
- ✅ Giới thiệu rõ bối cảnh, vấn đề thực tế
- ✅ Làm nổi bật tính sáng tạo (dual-RAG, multi-tenant, hierarchical chunking)
- ✅ Mục tiêu cụ thể, đo lường được
- ✅ Phạm vi rõ ràng, không quá tham vọng
- ✅ Phương pháp khoa học, có hệ thống
- ✅ Ý nghĩa thực tiễn cao
