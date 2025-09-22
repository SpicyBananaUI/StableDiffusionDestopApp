@echo off
REM Stable Diffusion Backend Launcher
REM No venv activation needed!

set PYTHON_PATH=C:\Users\Taylo\StableDiffusionDestopApp\installer\python-embedded\python.exe
set BACKEND_DIR=C:\Users\Taylo\StableDiffusionDestopApp\installer\backend

echo Starting Stable Diffusion Backend...
echo Python: %PYTHON_PATH%
echo Backend: %BACKEND_DIR%

REM Launch the backend
cd /d "%BACKEND_DIR%"
"%PYTHON_PATH%" launch_webui_backend.py

pause
