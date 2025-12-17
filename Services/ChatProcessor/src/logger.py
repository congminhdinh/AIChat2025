import logging
import os
from logging.handlers import TimedRotatingFileHandler
from datetime import datetime
import uuid
from contextvars import ContextVar

# Context variable to store session ID per request
session_context: ContextVar[str] = ContextVar('session_id', default='')

class SessionFormatter(logging.Formatter):
    """Custom formatter that includes session ID from context"""

    def format(self, record):
        # Get session ID from context
        session_id = session_context.get()
        record.session_id = session_id if session_id else ''
        return super().format(record)

def setup_logger():
    """Setup logger with TimedRotatingFileHandler"""

    # Create logs directory if it doesn't exist
    log_dir = os.path.join(os.path.dirname(os.path.dirname(__file__)), 'logs')
    os.makedirs(log_dir, exist_ok=True)

    # Create logger
    logger = logging.getLogger('chatprocessor')
    logger.setLevel(logging.INFO)

    # Clear existing handlers to avoid duplicates
    logger.handlers.clear()

    # Create TimedRotatingFileHandler (rotates daily at midnight)
    log_file = os.path.join(log_dir, 'chatprocessor.log')
    handler = TimedRotatingFileHandler(
        filename=log_file,
        when='midnight',
        interval=1,
        backupCount=30,  # Keep 30 days of logs
        encoding='utf-8'
    )

    # Set log format: {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SessionId} {Message:lj}{NewLine}{Exception}
    # Note: Python logging doesn't support timezone offset natively, using localtime
    formatter = SessionFormatter(
        fmt='%(asctime)s.%(msecs)03d [%(levelname)-3s] %(session_id)s %(message)s',
        datefmt='%Y-%m-%d %H:%M:%S'
    )
    handler.setFormatter(formatter)

    # Add handler to logger
    logger.addHandler(handler)

    # Also add console handler for debugging
    console_handler = logging.StreamHandler()
    console_handler.setFormatter(formatter)
    logger.addHandler(console_handler)

    return logger

# Initialize logger
logger = setup_logger()

def get_session_id():
    """Get or create session ID for current request"""
    session_id = session_context.get()
    if not session_id:
        session_id = str(uuid.uuid4())[:8]  # Use first 8 chars of UUID
        session_context.set(session_id)
    return session_id

def set_session_id(session_id: str):
    """Set session ID for current request"""
    session_context.set(session_id)

def clear_session_id():
    """Clear session ID after request"""
    session_context.set('')
