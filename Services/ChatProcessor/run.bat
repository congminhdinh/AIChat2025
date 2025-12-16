@echo off
REM Quick start script for ChatProcessor (Windows)

echo ==================================
echo ChatProcessor Quick Start
echo ==================================

REM Check if virtual environment exists
if not exist "venv\" (
    echo Creating virtual environment...
    python -m venv venv
)

REM Activate virtual environment
echo Activating virtual environment...
call venv\Scripts\activate.bat

REM Install dependencies
echo Installing dependencies...
pip install -r requirements.txt

REM Check if .env exists
if not exist ".env" (
    echo Creating .env file from .env.example...
    copy .env.example .env
    echo.
    echo WARNING: Please edit .env file with your configuration before running!
    echo          notepad .env
    pause
    exit /b 0
)

REM Run the service
echo Starting ChatProcessor...
python -m app.main

pause
