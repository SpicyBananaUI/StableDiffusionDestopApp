@echo off
REM Main Launcher - Starts both Backend and Frontend
REM This is the script that desktop shortcuts should point to

echo ======================================
echo   Stable Diffusion Desktop App      
echo ======================================

set "PYTHON_PATH=%~dp0python-embedded\python.exe"
set "BACKEND_DIR=%~dp0backend"
set "FRONTEND_EXE=%~dp0frontend\myApp.exe"

echo Starting Stable Diffusion Desktop App...
echo.

REM Check if Python exists
if not exist "%PYTHON_PATH%" (
    echo ERROR: Python not found at %PYTHON_PATH%
    echo Please ensure the installation is complete.
    pause
    exit /b 1
)

REM Check if frontend exists
if not exist "%FRONTEND_EXE%" (
    echo ERROR: Frontend not found at %FRONTEND_EXE%
    echo Please ensure the installation is complete.
    pause
    exit /b 1
)

echo [1/2] Starting backend server...
echo This may take 20-30 seconds on first launch...

REM Start backend in background
cd /d "%BACKEND_DIR%"
start "Stable Diffusion Backend" /min "%PYTHON_PATH%" -c "import sys; sys.path.insert(0, '.'); import launch_webui_backend"

echo [2/2] Starting desktop application...
echo Backend will be available at: http://localhost:7861
echo.

REM Wait a moment for backend to initialize
timeout /t 3 /nobreak >nul

REM Launch the frontend (this will be the main window)
"%FRONTEND_EXE%"

echo.
echo Application closed.
echo Backend server is still running in the background.
echo Close the backend window manually if needed.