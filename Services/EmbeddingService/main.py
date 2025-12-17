from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
import uvicorn
import time
import json
import uuid
from src.router import router
from src.config import settings
from src.logger import logger, set_session_id, clear_session_id
app = FastAPI(title='VN Law Embedding Service')

@app.middleware('http')
async def log_requests(request: Request, call_next):
    session_id = str(uuid.uuid4())[:8]
    set_session_id(session_id)
    try:
        body = await request.body()
        body_str = body.decode('utf-8') if body else ''
        query_params = dict(request.query_params)
        logger.info(f"Request: {request.method} {request.url.path} | Query: {(json.dumps(query_params) if query_params else 'None')} | Body: {(body_str[:200] if body_str else 'None')}")
    except Exception as e:
        logger.error(f'Error logging request: {str(e)}')
    start_time = time.time()
    try:
        response = await call_next(request)
        process_time = time.time() - start_time
        logger.info(f'Response: {response.status_code} | Process Time: {process_time:.3f}s')
        return response
    except Exception as e:
        process_time = time.time() - start_time
        logger.error(f'Exception: {str(e)} | Process Time: {process_time:.3f}s', exc_info=True)
        clear_session_id()
        return JSONResponse(status_code=500, content={'detail': str(e)})
    finally:
        clear_session_id()
app.include_router(router)
if __name__ == '__main__':
    logger.info('Starting VN Law Embedding Service...')
    uvicorn.run(app, host='0.0.0.0', port=8000)