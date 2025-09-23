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
cd /d "%BACKEND_DIR%"
start "Stable Diffusion Backend" /min "%PYTHON_PATH%" -c "import sys; sys.path.insert(0, '.'); import launch_webui_backend"
cd /d "%~dp0"

REM Wait for backend to be ready (poll http://localhost:7861)
set "READY=0"
set "RETRIES=60"
set "WAIT=2"
for /l %%i in (1,1,%RETRIES%) do (
    rem Query the models API which returns JSON once models are loaded
    powershell -Command "try { $r = Invoke-RestMethod -Uri 'http://localhost:7861/sdapi/v1/sd-models' -TimeoutSec 2 } catch { $r = $null }; if ($r -and $r.Count -gt 0) { exit 0 } else { exit 1 }"
    if not errorlevel 1 (
        set READY=1
        goto :backend_ready
    )
    echo Waiting for backend to be ready... (%%i/%RETRIES%)
    timeout /t %WAIT% /nobreak >nul
)

:backend_ready
if "%READY%"=="1" (
    echo Backend is ready!
) else (
    echo WARNING: Backend did not respond after %RETRIES% tries. Launching frontend anyway.
)

echo [2/2] Starting desktop application...
echo Backend will be available at: http://localhost:7861
echo.
"%FRONTEND_EXE%"

echo.
echo Application closed.
echo Backend server is still running in the background.
echo Close the backend window manually if needed.