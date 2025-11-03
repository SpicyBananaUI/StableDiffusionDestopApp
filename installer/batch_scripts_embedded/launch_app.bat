@echo off
REM Frontend-only Launcher (replaces the old combined launcher)

echo ======================================
echo   Stable Diffusion Desktop App      
echo ======================================

set "FRONTEND_EXE=%~dp0frontend\myApp.exe"

echo Starting Stable Diffusion Desktop App...
echo Frontend: %FRONTEND_EXE%

REM Check if frontend exists
if not exist "%FRONTEND_EXE%" (
    echo ERROR: Frontend not found at %FRONTEND_EXE%
    echo Please ensure the installation is complete.
    pause
    exit /b 1
)

echo.
echo Launching desktop application...
echo Backend will auto-start when needed by the frontend.
echo.

REM Launch the frontend
"%FRONTEND_EXE%"

REM If we get here, the frontend closed
echo.
echo Application closed.