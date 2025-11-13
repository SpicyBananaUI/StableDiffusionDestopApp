@echo off
REM Stable Diffusion Backend Launcher - Embedded Python Version

echo ======================================
echo   Stable Diffusion Backend Server    
echo ======================================

set "PYTHON_PATH=%~dp0python-embedded\python.exe"
set "BACKEND_DIR=%~dp0backend"

echo Starting Stable Diffusion Backend...
echo Python: %PYTHON_PATH%
echo Backend: %BACKEND_DIR%

REM Check if Python exists
if not exist "%PYTHON_PATH%" (
    echo ERROR: Python not found at %PYTHON_PATH%
    echo Please ensure the installation is complete.
    pause
    exit /b 1
)

echo.
echo Launching with CUDA GPU acceleration...
echo Backend will be available at: http://localhost:7860
echo Press Ctrl+C to stop the server.
echo.

REM Launch the backend with embedded Python
cd /d "%BACKEND_DIR%"
"%PYTHON_PATH%" -c "import sys; sys.path.insert(0, '.'); import launch_webui_backend"

REM If we get here, the backend stopped
echo.
echo Backend has stopped.
pause