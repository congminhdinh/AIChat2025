# AIChat2025 - Deployment Guide

## Prerequisites
- .NET 9 SDK
- Python 3.11+
- SQL Server
- RabbitMQ
- Minio (for storage)

## Local Development Setup

### 1. C# Services (dotnet)

```bash
# Restore and build all projects
dotnet restore
dotnet build

# Run individual services (each in separate terminal)
dotnet run --project ApiGateway
dotnet run --project Services/AccountService
dotnet run --project Services/TenantService
dotnet run --project Services/DocumentService
dotnet run --project Services/ChatService
dotnet run --project Services/StorageService
dotnet run --project AdminCMS
dotnet run --project WebApp
```

### 2. Python Services (venv)

#### EmbeddingService
```bash
cd Services/EmbeddingService
python -m venv venv
venv\Scripts\activate      # Windows
# source venv/bin/activate  # Linux/Mac
pip install -r requirements.txt
python main.py
```

#### ChatProcessor
```bash
cd Services/ChatProcessor
python -m venv venv
venv\Scripts\activate      # Windows
# source venv/bin/activate  # Linux/Mac
pip install -r requirements.txt
python main.py
```

## Configuration

Update connection strings in `Config/appsettings.json` for each service:
- AccountService
- TenantService
- DocumentService
- ChatService

## Service Ports (Default)
- ApiGateway: 5002
- AdminCMS: 7263
- WebApp: 7262
- EmbeddingService: 8000
- ChatProcessor: 8001
