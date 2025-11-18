@echo off
REM Stable Diffusion Backend Launcher - Embedded Python Version

echo ======================================
echo   Stable Diffusion Backend Server    
echo ======================================

set "PYTHON_PATH=%~dp0python-embedded\python.exe"
set "BACKEND_DIR=%~dp0backend"
set "PYTHONPATH=%BACKEND_DIR%;%PYTHONPATH%"

REM Ensure backend model directories exist inside the installation root
if not exist "%BACKEND_DIR%\models" mkdir "%BACKEND_DIR%\models"
if not exist "%BACKEND_DIR%\models\Stable-diffusion" mkdir "%BACKEND_DIR%\models\Stable-diffusion"
if not exist "%BACKEND_DIR%\models\VAE" mkdir "%BACKEND_DIR%\models\VAE"
if not exist "%BACKEND_DIR%\models\LoRA" mkdir "%BACKEND_DIR%\models\LoRA"

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
"%PYTHON_PATH%" "%BACKEND_DIR%\launch_webui_backend.py" %*

REM If we get here, the backend stopped
echo.
echo Backend has stopped.
pause