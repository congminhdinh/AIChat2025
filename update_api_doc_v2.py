# -*- coding: utf-8 -*-
"""
Script to update API Specification document with detailed DTO properties
Version 2: Insert new rows for each property instead of multi-line cells
"""
from docx import Document
from docx.shared import Pt, Inches
from docx.oxml.ns import qn
from docx.oxml import OxmlElement
from copy import deepcopy

# Define DTO structures based on source code analysis
DTO_DEFINITIONS = {
    # ============================================
    # C# Services DTOs
    # ============================================

    # TokenDto (used as LoginResponseDto)
    "LoginResponseDto": [
        ("AccessToken", "string", "JWT access token"),
        ("RefreshToken", "string", "Refresh token để làm mới access token"),
        ("ExpiresAt", "DateTime", "Thời điểm hết hạn của access token"),
    ],

    # AccountDTO
    "AccountDTO": [
        ("Id", "int", "ID tài khoản"),
        ("Name", "string?", "Tên người dùng"),
        ("Email", "string", "Email tài khoản"),
        ("AvatarUrl", "string?", "URL ảnh đại diện"),
        ("PermissionList", "List<int>", "Danh sách quyền của tài khoản"),
        ("IsActive", "bool", "Trạng thái kích hoạt"),
    ],

    # TenantDTO
    "TenantDTO": [
        ("Id", "int", "ID của tenant"),
        ("Name", "string", "Tên tenant"),
        ("Description", "string?", "Mô tả tenant"),
        ("IsActive", "bool", "Trạng thái kích hoạt"),
        ("CreatedAt", "DateTime", "Thời điểm tạo"),
        ("ModifiedAt", "DateTime", "Thời điểm cập nhật cuối"),
    ],

    # ConversationDTO
    "ConversationDTO": [
        ("Id", "int", "ID cuộc hội thoại"),
        ("Title", "string", "Tiêu đề cuộc hội thoại"),
        ("CreatedAt", "DateTime", "Thời điểm tạo"),
        ("LastMessageAt", "DateTime", "Thời điểm tin nhắn cuối"),
        ("MessageCount", "int", "Số lượng tin nhắn"),
        ("Messages", "List<MessageDTO>", "Danh sách tin nhắn (khi lấy chi tiết)"),
    ],

    # MessageDTO
    "MessageDTO": [
        ("Id", "int", "ID tin nhắn"),
        ("ConversationId", "int", "ID cuộc hội thoại"),
        ("RequestId", "int", "ID request gốc (nếu là response)"),
        ("Content", "string", "Nội dung tin nhắn"),
        ("Timestamp", "DateTime", "Thời điểm gửi"),
        ("IsBot", "bool", "True nếu là tin nhắn từ bot"),
        ("UserId", "int", "ID người dùng gửi"),
        ("FeedbackId", "int", "ID feedback (nếu có)"),
        ("Ratings", "short", "Đánh giá (1=like, 2=dislike)"),
        ("Type", "ChatType", "Loại tin nhắn (Request=0, Response=1)"),
    ],

    # ChatFeedbackDTO
    "ChatFeedbackDTO": [
        ("Id", "int", "ID feedback"),
        ("Message", "string", "Nội dung câu hỏi gốc"),
        ("Response", "string", "Nội dung câu trả lời"),
        ("Ratings", "short", "Đánh giá (1=like, 2=dislike)"),
        ("Content", "string", "Nội dung phản hồi chi tiết"),
        ("Category", "ChatFeedbackCategory", "Phân loại feedback"),
    ],

    # SystemPromptDTO
    "SystemPromptDTO": [
        ("Id", "int", "ID system prompt"),
        ("Name", "string", "Tên prompt"),
        ("Content", "string", "Nội dung prompt"),
        ("Description", "string?", "Mô tả prompt"),
        ("IsActive", "bool", "Trạng thái kích hoạt"),
    ],

    # PromptConfigDTO
    "PromptConfigDTO": [
        ("Id", "int", "ID cấu hình"),
        ("Key", "string", "Khóa cấu hình"),
        ("Value", "string", "Giá trị cấu hình"),
    ],

    # DocumentDTO
    "DocumentDTO": [
        ("Id", "int", "ID tài liệu"),
        ("FileName", "string", "Tên file gốc"),
        ("DocumentName", "string?", "Tên tài liệu hiển thị"),
        ("FilePath", "string", "Đường dẫn file"),
        ("Action", "DocumentAction", "Trạng thái xử lý (Upload, Vectorize_Success,...)"),
        ("LastModifiedAt", "DateTime?", "Thời điểm cập nhật cuối"),
        ("TenantId", "int", "ID tenant sở hữu"),
    ],

    # FileUploadResponseDTO
    "FileUploadResponseDTO": [
        ("FilePath", "string", "Đường dẫn file đã upload"),
        ("FileName", "string", "Tên file"),
    ],

    # BaseResponse (non-generic)
    "BaseResponse": [
        ("Status", "BaseResponseStatus", "Trạng thái (Success=1, Error=0)"),
        ("Message", "string", "Thông điệp phản hồi"),
    ],

    # ============================================
    # ChatProcessor Service DTOs (Python)
    # ============================================

    # ChatRequest - Input cho /api/chat/test
    "ChatRequest": [
        ("conversation_id", "int", "ID cuộc hội thoại"),
        ("message", "string", "Nội dung tin nhắn"),
        ("user_id", "int", "ID người dùng"),
        ("tenant_id", "int", "ID tenant"),
        ("system_instruction", "List<PromptConfigDto>?", "Danh sách cấu hình prompt (tùy chọn)"),
    ],

    # ChatResponse - Output cho /api/chat/test
    "ChatResponse": [
        ("conversation_id", "int", "ID cuộc hội thoại"),
        ("message", "string", "Nội dung phản hồi từ bot"),
        ("user_id", "int", "ID người dùng"),
        ("timestamp", "datetime", "Thời điểm phản hồi"),
        ("model_used", "string", "Tên model LLM đã sử dụng"),
        ("rag_documents_used", "int", "Số lượng tài liệu RAG được sử dụng"),
        ("source_ids", "List<int>?", "Danh sách ID nguồn tài liệu"),
        ("scenario", "string?", "Kịch bản xử lý (BOTH, COMPANY_ONLY, LEGAL_ONLY, NONE)"),
    ],

    # BatchTestRequest - Input cho /api/test/batch
    "BatchTestRequest": [
        ("entities", "List<TestEntity>", "Danh sách các test case:"),
    ],

    # TestEntity - Nested trong BatchTestRequest
    "TestEntity": [
        ("tenant_id", "int", "ID tenant"),
        ("TC_id", "string", "ID của test case"),
        ("questions", "string", "Câu hỏi cần test"),
    ],

    # BatchTestResponse - Output cho /api/test/batch
    "BatchTestResponse": [
        ("status", "string", "Trạng thái (accepted)"),
        ("message", "string", "Thông báo xử lý"),
        ("output_file", "string", "Tên file output (tdd.json)"),
    ],

    # HealthCheckResponse - Output cho /health (ChatProcessor)
    "HealthCheckResponse": [
        ("status", "string", "Trạng thái chung (healthy/degraded)"),
        ("ollama", "bool", "Trạng thái kết nối Ollama"),
        ("qdrant", "bool", "Trạng thái kết nối Qdrant"),
    ],

    # ============================================
    # EmbeddingService DTOs (Python)
    # ============================================

    # EmbeddingRequest - Input cho /embed
    "EmbeddingRequest": [
        ("text", "string", "Văn bản cần tạo embedding"),
    ],

    # EmbeddingResponse - Output cho /embed và /search
    "EmbeddingResponse": [
        ("vector", "List<float>", "Vector embedding"),
        ("dimensions", "int", "Số chiều của vector"),
    ],

    # VectorizeRequest - Input cho /vectorize
    "VectorizeRequest": [
        ("text", "string", "Văn bản cần vector hóa"),
        ("metadata", "dict", "Metadata kèm theo (source_id, tenant_id, type,...)"),
        ("collection_name", "string?", "Tên collection trong Qdrant (tùy chọn)"),
    ],

    # VectorizeResponse - Output cho /vectorize, /vectorize-batch, /api/embeddings/delete
    "VectorizeResponse": [
        ("success", "bool", "Trạng thái thành công"),
        ("point_id", "string?", "ID của point trong Qdrant"),
        ("count", "int?", "Số lượng vector đã xử lý (batch)"),
        ("dimensions", "int?", "Số chiều của vector"),
        ("collection", "string", "Tên collection"),
        ("message", "string?", "Thông báo kết quả"),
    ],

    # BatchVectorizeRequest - Input cho /vectorize-batch
    "BatchVectorizeRequest": [
        ("items", "List<VectorizeRequest>", "Danh sách các item cần vector hóa:"),
        ("collection_name", "string?", "Tên collection trong Qdrant (tùy chọn)"),
    ],

    # DeleteRequest - Input cho /api/embeddings/delete
    "DeleteRequest": [
        ("source_id", "int", "ID nguồn tài liệu cần xóa"),
        ("tenant_id", "int", "ID tenant"),
        ("type", "int", "Loại tài liệu"),
        ("collection_name", "string?", "Tên collection (tùy chọn)"),
    ],

    # SearchRequest - Input cho /search
    "SearchRequest": [
        ("query", "string", "Câu truy vấn tìm kiếm"),
        ("tenant_id", "int", "ID tenant"),
        ("limit", "int", "Số kết quả tối đa"),
        ("score_threshold", "float", "Ngưỡng điểm tương đồng"),
    ],

    # EmbeddingHealthResponse - Output cho /health (EmbeddingService)
    "EmbeddingHealthResponse": [
        ("status", "string", "Trạng thái (ok)"),
        ("model", "string", "Tên model embedding đang dùng"),
        ("qdrant", "string", "Địa chỉ Qdrant server"),
    ],
}

# PaginatedResult structure
PAGINATED_RESULT = [
    ("PageIndex", "int", "Số trang hiện tại"),
    ("TotalPages", "int", "Tổng số trang"),
    ("PageSize", "int", "Số phần tử mỗi trang"),
]


def expand_paginated_result(inner_type):
    """Expand PaginatedResult<T> into full property list"""
    result = list(PAGINATED_RESULT)

    inner_dto = DTO_DEFINITIONS.get(inner_type)
    if inner_dto:
        result.append(("Items", f"List<{inner_type}>", f"Danh sách {inner_type}:"))
        for prop in inner_dto:
            result.append((f"  └ {prop[0]}", prop[1], prop[2]))
    else:
        result.append(("Items", f"List<{inner_type}>", f"Danh sách {inner_type}"))

    return result


def expand_list_type(inner_type):
    """Expand List<T> into full property list"""
    inner_dto = DTO_DEFINITIONS.get(inner_type)
    if inner_dto:
        result = [(f"[Mỗi phần tử là {inner_type}]", "", "")]
        for prop in inner_dto:
            result.append((f"  └ {prop[0]}", prop[1], prop[2]))
        return result
    return None


def get_expanded_properties(type_text):
    """Get expanded properties for a type"""
    type_text = type_text.strip()

    # Handle PaginatedResult<T>
    if type_text.startswith("PaginatedResult<") and type_text.endswith(">"):
        inner = type_text[16:-1]
        return expand_paginated_result(inner)

    # Handle List<T> with DTO or Request/Response suffix
    if type_text.startswith("List<") and type_text.endswith(">"):
        inner = type_text[5:-1]
        if "DTO" in inner or "Request" in inner or "Response" in inner or "Entity" in inner:
            return expand_list_type(inner)

    # Handle direct DTO types
    if type_text in DTO_DEFINITIONS:
        return DTO_DEFINITIONS[type_text]

    # Handle TypeName[] array syntax (e.g., VectorizeRequest[])
    if type_text.endswith("[]"):
        inner = type_text[:-2]
        if inner in DTO_DEFINITIONS:
            return expand_list_type(inner)

    # Handle TypeName {...} syntax (e.g., "ChatResponse {conversation_id, message,...}")
    if " {" in type_text or "{" in type_text:
        # Extract type name before the brace
        type_name = type_text.split("{")[0].strip().split(" ")[0]
        if type_name in DTO_DEFINITIONS:
            return DTO_DEFINITIONS[type_name]

    # Handle pattern like "[{tenant_id" which is JSON array description -> BatchTestRequest.entities
    if type_text.startswith("[{"):
        # This is likely an array of objects - check for TestEntity pattern
        if "tenant_id" in type_text.lower():
            return expand_list_type("TestEntity")

    return None


def insert_row_after(table, row_idx, values, col_indices):
    """Insert a new row after the specified index with given values"""
    # Get the row element
    tbl = table._tbl
    tr = table.rows[row_idx]._tr

    # Create new row element
    new_tr = deepcopy(tr)

    # Clear and set new values
    new_row_cells = new_tr.findall(qn('w:tc'))

    name_col_idx, type_col_idx, desc_col_idx = col_indices

    for idx, tc in enumerate(new_row_cells):
        # Find or create paragraph
        p = tc.find(qn('w:p'))
        if p is None:
            p = OxmlElement('w:p')
            tc.append(p)
        else:
            # Clear existing runs
            for r in p.findall(qn('w:r')):
                p.remove(r)

        # Add new run with text
        r = OxmlElement('w:r')
        t = OxmlElement('w:t')

        if idx == name_col_idx:
            t.text = values[0]
        elif idx == type_col_idx:
            t.text = values[1]
        elif idx == desc_col_idx:
            t.text = values[2]
        else:
            t.text = ""

        r.append(t)
        p.append(r)

    # Insert after the current row
    tr.addnext(new_tr)


def process_document(input_path, output_path):
    """Process the Word document and expand DTO types with new rows"""
    doc = Document(input_path)

    total_rows_added = 0
    tables_modified = 0

    for table_idx, table in enumerate(doc.tables):
        if len(table.rows) < 2:
            continue

        # Check header row
        header_row = table.rows[0]
        header_texts = [cell.text.strip() for cell in header_row.cells]

        # Find column indices
        type_col_idx = None
        name_col_idx = None
        desc_col_idx = None

        for idx, text in enumerate(header_texts):
            if 'Kiểu dữ liệu' in text:
                type_col_idx = idx
            elif 'Tên biến' in text:
                name_col_idx = idx
            elif 'Giải thích' in text:
                desc_col_idx = idx

        if type_col_idx is None:
            continue

        col_indices = (name_col_idx, type_col_idx, desc_col_idx)

        # Collect rows that need expansion (process in reverse order)
        rows_to_expand = []

        for row_idx in range(1, len(table.rows)):
            row = table.rows[row_idx]
            cells = row.cells

            if type_col_idx >= len(cells):
                continue

            type_text = cells[type_col_idx].text.strip()
            expanded = get_expanded_properties(type_text)

            if expanded:
                rows_to_expand.append((row_idx, type_text, expanded))

        if not rows_to_expand:
            continue

        tables_modified += 1

        # Process in reverse order to maintain correct indices
        for row_idx, type_text, expanded in reversed(rows_to_expand):
            row = table.rows[row_idx]
            cells = row.cells

            # Update the original row to show it's being expanded
            if name_col_idx is not None and name_col_idx < len(cells):
                cells[name_col_idx].text = f"[{type_text}]"
            cells[type_col_idx].text = "object"
            if desc_col_idx is not None and desc_col_idx < len(cells):
                cells[desc_col_idx].text = f"Cấu trúc {type_text}:"

            # Insert new rows for each property (in reverse order)
            for prop in reversed(expanded):
                insert_row_after(table, row_idx, prop, col_indices)
                total_rows_added += 1

    # Save the modified document
    doc.save(output_path)
    return tables_modified, total_rows_added


def main():
    input_file = r"C:\Users\MINH.DC\source\repos\AIChat2025\API_Specification_AIChat2025.docx"
    output_file = r"C:\Users\MINH.DC\source\repos\AIChat2025\API_Specification_AIChat2025_Updated.docx"

    print(f"Processing: {input_file}")
    print("=" * 60)

    tables_modified, rows_added = process_document(input_file, output_file)

    print(f"Tables modified: {tables_modified}")
    print(f"Rows added: {rows_added}")
    print("=" * 60)
    print(f"Output saved to: {output_file}")


if __name__ == "__main__":
    main()
