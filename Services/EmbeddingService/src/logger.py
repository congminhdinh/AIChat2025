import logging
import os
from logging.handlers import TimedRotatingFileHandler
from datetime import datetime
import uuid
from contextvars import ContextVar
session_context: ContextVar[str] = ContextVar('session_id', default='')

class SessionFormatter(logging.Formatter):

    def format(self, record):
        session_id = session_context.get()
        record.session_id = session_id
        return super().format(record)

def setup_logger():
    log_dir = os.path.join(os.path.dirname(os.path.dirname(__file__)), 'logs')
    os.makedirs(log_dir, exist_ok=True)
    logger = logging.getLogger('embedding_service')
    logger.setLevel(logging.INFO)
    logger.handlers.clear()
    log_file = os.path.join(log_dir, 'embedding_service.log')
    handler = TimedRotatingFileHandler(filename=log_file, when='midnight', interval=1, backupCount=30, encoding='utf-8')
    formatter = SessionFormatter(fmt='%(asctime)s.%(msecs)03d [%(levelname)-3s] %(session_id)s %(message)s', datefmt='%Y-%m-%d %H:%M:%S')
    handler.setFormatter(formatter)
    logger.addHandler(handler)
    console_handler = logging.StreamHandler()
    console_handler.setFormatter(formatter)
    logger.addHandler(console_handler)
    return logger
logger = setup_logger()

def get_session_id():
    session_id = session_context.get()
    if not session_id:
        session_id = str(uuid.uuid4())[:8]
        session_context.set(session_id)
    return session_id

def set_session_id(session_id: str):
    session_context.set(session_id)

def clear_session_id():
    session_context.set('')