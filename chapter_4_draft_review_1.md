Đánh giá draft chapter 4 v2:
-embeddingservice sử dụng truro7/vn-law-embedding trên huggingface , chỉnh sửa luồng embedding và luồng chat (chat processor gọi sang embeddingservice để search)
-phần vector DbQdrant:\textbf{C. Vector Database Schema (Qdrant)}
Qdrant được sử dụng để lưu trữ vector embeddings với cấu trúc collections gồm: chỉ có vn_law_documents chứa tất cả các vector
ví dụ metadata cho mỗi vecto: {
"text":"Chương III Mục 1. CÔNG TY TRÁCH NHIỆM HỮU HẠN HAI …"
"source_id":1049
"file_name":"59_2020_QH14_427301.docx"// tên file
"document_name":"Luật doanh nghiệp 2020" tên tài liệu
"father_doc_name":""// tên tài liệu cha (tên tài liệu luật nếu tài liệu trong vecto là thuộc nghị định)
"heading1":"Chương III"
"heading2":"Mục 1. CÔNG TY TRÁCH NHIỆM HỮU HẠN HAI THÀNH VIÊN …"
"content":"Điều 61. Thủ tục thông qua nghị quyết, quyết định …"
"tenant_id":1
"type":1 //0 - tài liệu doanh nghiệp, 1- luật, 2- nghị định)
}
- khi lấy luật quốc gia (tenant 1)trong db vecto, sẽ lấy kèm cả luật và nghị định. 
