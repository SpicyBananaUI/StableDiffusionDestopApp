@echo off
echo Starting FastAPI server...

REM Activate the virtual environment
call venv\Scripts\activate

REM Run the FastAPI server
uvicorn main:app --reload

REM If the server is closed, deactivate the virtual environment
call venv\Scripts\deactivate
