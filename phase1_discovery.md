# PHASE 1: Project Structure Discovery

## MỤC TIÊU
Quét toàn bộ cấu trúc thư mục dự án AIChat2025 và tạo bản đồ đầy đủ về tất cả services, projects và vị trí của chúng.

## BỐI CẢNH DỰ ÁN
AIChat2025 là hệ thống RAG đa thuê bao (multi-tenant) với:
- **Backend**: .NET 9 microservices
- **Frontend**: ASP.NET MVC applications
- **AI Layer**: Python services
- **Key Features**: Multi-tenant isolation, RAG pipeline cho tư vấn văn bản pháp lý

## CÁC BƯỚC THỰC HIỆN

### BƯỚC 1: Xác định thư mục gốc
```bash
# In ra thư mục hiện tại
pwd

# Liệt kê tất cả thư mục con cấp 1
ls -la
```

**Output mẫu:**
```
Current directory: /home/user/AIChat2025
Subdirectories: src/, docs/, tests/, scripts/
```

---

### BƯỚC 2: Tìm tất cả .NET projects

```bash
# Tìm tất cả file .csproj
find . -name "*.csproj" -type f

# Với MỖI file .csproj tìm được:
# 1. Đọc tên project (từ tên file)
# 2. Kiểm tra loại project:
#    - Nếu chứa "Microsoft.AspNetCore.Mvc" → Frontend (ASP.NET MVC)
#    - Nếu chứa "Microsoft.AspNetCore" nhưng KHÔNG có "Mvc" → Backend (WebAPI)
#    - Nếu chứa "Yarp" hoặc "ReverseProxy" → API Gateway
```

**Output cho MỖI project:**
```markdown
### [Project Name]
- **Full Path**: `./src/Services/AccountService/AccountService.csproj`
- **Type**: [WebAPI / ASP.NET MVC / Gateway]
- **Framework**: net9.0 (đọc từ <TargetFramework> trong .csproj)
- **Key Packages**: (liệt kê 3-5 packages quan trọng nhất từ .csproj)
  - Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
  - Microsoft.EntityFrameworkCore.SqlServer (9.0.0)
  - ...
```

---

### BƯỚC 3: Tìm tất cả Python services

```bash
# Tìm tất cả file requirements.txt hoặc pyproject.toml
find . -name "requirements.txt" -o -name "pyproject.toml" -type f

# Với MỖI file tìm được:
# 1. Lấy tên thư mục chứa (= tên service)
# 2. Tìm file main entry point:
find [service_dir] -name "main.py" -o -name "__main__.py" -o -name "app.py" | head -1
```

**Output cho MỖI Python service:**
```markdown
### [Service Name]
- **Full Path**: `./src/AI/ChatProcessor/`
- **Entry Point**: `./src/AI/ChatProcessor/src/main.py`
- **Requirements File**: `./src/AI/ChatProcessor/requirements.txt`
- **Key Dependencies**: (đọc từ requirements.txt, liệt kê 5-7 quan trọng nhất)
  - fastapi==0.109.0
  - qdrant-client==1.7.0
  - sentence-transformers==2.3.1
  - ...
```

---

### BƯỚC 4: Xác định API Gateway

```bash
# Tìm file chứa cấu hình YARP
find . -name "yarp.json" -type f
find . -name "appsettings.json" -type f -exec grep -l "yarp\|ReverseProxy" {} \;

# Output project chứa YARP
```

---

### BƯỚC 5: Xác định Database connections

```bash
# Tìm connection strings trong appsettings.json
find . -name "appsettings.json" -type f -exec grep -l "ConnectionStrings" {} \;

# Với MỖI file tìm được, extract:
# - Database type (SQL Server / PostgreSQL / MySQL)
# - Database name
```

---

## ĐỊNH DẠNG OUTPUT

Tạo file markdown với cấu trúc cây (text-based tree) như sau:

```markdown
# AIChat2025 - Project Structure Map

**Scanned at**: [Ngày giờ hiện tại]
**Root Directory**: [Đường dẫn tuyệt đối]

---

## 📊 TỔNG QUAN DỰ ÁN

```
AIChat2025/
├── src/
│   ├── Services/          (Backend Microservices)
│   │   ├── AccountService/
│   │   ├── TenantService/
│   │   ├── ChatService/
│   │   ├── DocumentService/
│   │   └── StorageService/
│   ├── Frontend/          (ASP.NET MVC Apps)
│   │   ├── WebApp/
│   │   └── AdminCMS/
│   ├── Gateway/           (API Gateway)
│   │   └── Gateway/
│   └── AI/                (Python Services)
│       ├── ChatProcessor/
│       └── EmbeddingService/
├── tests/
└── docs/
```

---

## 🔧 .NET PROJECTS

### Backend Microservices

#### 1. AccountService
```
Path: ./src/Services/AccountService/AccountService.csproj
Type: WebAPI
Framework: net9.0

Key Packages:
├── Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
├── Microsoft.EntityFrameworkCore.SqlServer (9.0.0)
├── AutoMapper.Extensions.Microsoft.DependencyInjection (12.0.0)
└── Swashbuckle.AspNetCore (6.5.0)

Connection Strings:
└── DefaultConnection → SQL Server database "AIChat_Account"
```

#### 2. TenantService
```
Path: ./src/Services/TenantService/TenantService.csproj
Type: WebAPI
Framework: net9.0

Key Packages:
├── Microsoft.EntityFrameworkCore.SqlServer (9.0.0)
├── FluentValidation.AspNetCore (11.3.0)
└── MediatR (12.0.0)

Connection Strings:
└── DefaultConnection → SQL Server database "AIChat_Tenant"
```

[... tiếp tục cho TẤT CẢ backend services ...]

---

### Frontend Projects

#### 1. WebApp (Tenant Portal)
```
Path: ./src/Frontend/WebApp/WebApp.csproj
Type: ASP.NET MVC
Framework: net9.0

Key Packages:
├── Microsoft.AspNetCore.Mvc (9.0.0)
├── Microsoft.AspNetCore.Authentication.Cookies (8.0.0)
└── Newtonsoft.Json (13.0.3)

API Gateway Connection:
└── Configured in appsettings.json → "https://api.aichat.vn"
```

#### 2. AdminCMS (Admin Portal)
```
Path: ./src/Frontend/AdminCMS/AdminCMS.csproj
Type: ASP.NET MVC
Framework: net9.0

Key Packages:
├── Microsoft.AspNetCore.Mvc (9.0.0)
└── [...]

API Gateway Connection:
└── Configured in appsettings.json → "https://admin-api.aichat.vn"
```

---

### API Gateway

#### Gateway
```
Path: ./src/Gateway/Gateway/Gateway.csproj
Type: YARP Reverse Proxy
Framework: net9.0

Key Packages:
├── Yarp.ReverseProxy (2.1.0)
├── Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
└── [...]

Configuration:
├── yarp.json (hoặc appsettings.json section "ReverseProxy")
└── Routes to:
    ├── /api/account/* → AccountService
    ├── /api/tenant/* → TenantService
    ├── /api/chat/* → ChatService
    ├── /api/document/* → DocumentService
    └── /api/storage/* → StorageService
```

---

## 🐍 PYTHON SERVICES

### 1. ChatProcessor
```
Path: ./src/AI/ChatProcessor/
Entry: ./src/AI/ChatProcessor/src/main.py
Requirements: ./src/AI/ChatProcessor/requirements.txt

Folder Structure:
ChatProcessor/
├── src/
│   ├── main.py
│   ├── api/             (FastAPI routes)
│   ├── services/        (Business logic)
│   ├── models/          (Data models)
│   ├── clients/         (External clients: Qdrant, Ollama, RabbitMQ)
│   └── utils/
├── tests/
├── requirements.txt
└── Dockerfile

Key Dependencies:
├── fastapi==0.109.0
├── uvicorn==0.27.0
├── qdrant-client==1.7.0
├── sentence-transformers==2.3.1
├── langchain==0.1.0
├── pika==1.3.2 (RabbitMQ)
└── python-dotenv==1.0.0

External Connections:
├── Qdrant Vector DB → http://localhost:6333
├── Ollama LLM → http://localhost:11434
└── RabbitMQ → localhost:5672 (queue: chat_queue)
```

### 2. EmbeddingService
```
Path: ./src/AI/EmbeddingService/
Entry: ./src/AI/EmbeddingService/src/main.py
Requirements: ./src/AI/EmbeddingService/requirements.txt

Folder Structure:
EmbeddingService/
├── src/
│   ├── main.py
│   ├── api/
│   ├── services/        (Chunking, Embedding, Enrichment)
│   ├── models/
│   ├── clients/         (Qdrant, MinIO, RabbitMQ)
│   └── utils/
├── tests/
├── requirements.txt
└── Dockerfile

Key Dependencies:
├── fastapi==0.109.0
├── qdrant-client==1.7.0
├── sentence-transformers==2.3.1
├── minio==7.2.0
├── pika==1.3.2
└── pypdf==3.17.0

External Connections:
├── Qdrant Vector DB → http://localhost:6333
├── MinIO Object Storage → http://localhost:9000
└── RabbitMQ → localhost:5672 (queue: document_queue)
```

---

## 📊 THỐNG KÊ TỔNG QUAN

### Projects Found
- **Total .NET Projects**: [số lượng]
  - Backend Services: [số lượng]
  - Frontend Projects: [số lượng]
  - API Gateway: 1
- **Total Python Services**: [số lượng]

### Technology Stack
```
Backend:
├── .NET 9 (C# 12)
├── Entity Framework Core 9.0
├── YARP 2.1 (API Gateway)
└── SQL Server 2022

AI Layer:
├── Python 3.11
├── FastAPI 0.109
├── Sentence Transformers 2.3
└── LangChain 0.1

Data & Infrastructure:
├── SQL Server 2022 (Relational data)
├── Qdrant 1.7 (Vector database)
├── MinIO (Object storage)
├── Ollama (LLM runtime - Qwen2.5 7B)
└── RabbitMQ (Message queue)
```

### File Statistics
- Total .csproj files: [số lượng]
- Total .py files: [số lượng]
- Total lines of code: [ước tính nếu có thể]

---

## ✅ VERIFICATION CHECKLIST

Kiểm tra các yếu tố sau đã được tìm thấy:

- [ ] AccountService (Backend)
- [ ] TenantService (Backend)
- [ ] ChatService (Backend)
- [ ] DocumentService (Backend)
- [ ] StorageService (Backend)
- [ ] Gateway (YARP)
- [ ] WebApp (Frontend)
- [ ] AdminCMS (Frontend)
- [ ] ChatProcessor (Python)
- [ ] EmbeddingService (Python)

Nếu thiếu service nào, ghi rõ: **[NOT FOUND]**

---

## 🔍 NOTES

- Liệt kê bất kỳ cấu trúc bất thường hoặc không theo chuẩn
- Ghi chú về các dependencies đặc biệt hoặc cấu hình phức tạp
- Đề xuất (nếu có) về việc tối ưu cấu trúc project
```

---

## YÊU CẦU QUAN TRỌNG

1. ✅ **Sử dụng cấu trúc text-based tree** (dùng ├──, └──, │) để vẽ cây thư mục
2. ✅ **KHÔNG dùng PlantUML, Mermaid hay bất kỳ diagram code nào**
3. ✅ **Output phải là markdown thuần** có thể đọc trực tiếp
4. ✅ **Đường dẫn phải CHÍNH XÁC** như trong file system
5. ✅ **Liệt kê ĐẦY ĐỦ** tất cả projects tìm thấy, không bỏ sót
6. ✅ **Nếu không tìm thấy file/folder mong đợi** → Ghi rõ [NOT FOUND]

---

## BẮT ĐẦU QUÉT

Hãy bắt đầu quét từ thư mục hiện tại và tạo file `project_map.md` với format như trên.
