@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  Stable Diffusion Desktop App
echo ========================================
echo.

:: Set paths
set "CURRENT_DIR=%~dp0"
set "BACKEND_DIR=%CURRENT_DIR%backend"
set "VENV_DIR=%BACKEND_DIR%\webui-venv"
set "PYTHON_EXE=%VENV_DIR%\Scripts\python.exe"
set "SD_MODELS_DIR=%BACKEND_DIR%\models\Stable-diffusion"

:: Check if virtual environment exists
if not exist "%PYTHON_EXE%" (
    echo ERROR: Virtual environment not found!
    echo Expected location: %VENV_DIR%
    echo Please ensure the installer completed successfully.
    pause
    exit /b 1
)

:: Check for models
echo Checking for Stable Diffusion models...
if exist "%SD_MODELS_DIR%\*.safetensors" (
    echo Models found! Starting backend...
    goto :start_backend
) else (
    echo No models found in: %SD_MODELS_DIR%
    echo.
    echo Would you like to download a model now?
    echo 1^) Download DreamShaper 8 ^(recommended, ~2GB^)
    echo 2^) Download SDXL Base 1.0 ^(larger, ~7GB^) 
    echo 3^) Skip and start without models
    echo.
    set /p "choice=Enter your choice (1-3): "
    
    if "!choice!"=="1" goto :download_dreamshaper
    if "!choice!"=="2" goto :download_sdxl
    if "!choice!"=="3" goto :start_backend
    
    echo Invalid choice. Starting without models...
    goto :start_backend
)

:download_dreamshaper
echo.
echo Downloading DreamShaper 8... Please wait.
mkdir "%SD_MODELS_DIR%" 2>nul
powershell.exe -ExecutionPolicy Bypass -Command "Invoke-WebRequest -Uri 'https://huggingface.co/Lykon/DreamShaper/resolve/main/DreamShaper_8_pruned.safetensors' -OutFile '%SD_MODELS_DIR%\dreamshaper_8.safetensors' -UseBasicParsing; Write-Host 'Download completed!'"
goto :start_backend

:download_sdxl
echo.
echo Downloading SDXL Base 1.0... Please wait.
mkdir "%SD_MODELS_DIR%" 2>nul
powershell.exe -ExecutionPolicy Bypass -Command "Invoke-WebRequest -Uri 'https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0/resolve/main/sd_xl_base_1.0.safetensors' -OutFile '%SD_MODELS_DIR%\sd_xl_base_1.0.safetensors' -UseBasicParsing; Write-Host 'Download completed!'"
goto :start_backend

:start_backend
echo.
echo Starting Stable Diffusion backend...
cd /d "%BACKEND_DIR%"
call "%VENV_DIR%\Scripts\activate.bat"
"%PYTHON_EXE%" launch_webui_backend.py
pause
