from fastapi import FastAPI
import uvicorn

from src.router import router
from src.config import settings

app = FastAPI(title="VN Law Embedding Service")
app.include_router(router)

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
